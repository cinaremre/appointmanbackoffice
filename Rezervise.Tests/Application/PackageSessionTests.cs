using Rezervise.Application.Features.Packages.Commands.ConsumePackageSession;
using Rezervise.Application.Features.Packages.Commands.PurchasePackage;
using Rezervise.Application.Features.Packages.Commands.ReleasePackageSession;
using Rezervise.Application.Features.Packages.Commands.ReservePackageSession;
using Rezervise.Application.Features.Packages.Queries.GetCustomerPackageBalance;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// Seans paketi (dokuman Modul 5) akis testleri: rezervasyon, tuketim,
/// serbest birakma, bakiye hesabi ve cift islem/hak asimi korumalari.
/// </summary>
public class PackageSessionTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    private static readonly DateTime NowUtc = new(2026, 11, 21, 8, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// SUT paketi: ayni repo ornekleriyle olusturulmus handler'lar ve
    /// testin ilerleyisinde erisilmesi gereken repo'lar.
    /// Rezervasyon/tuketim/serbest birakma islemlerini paket musterisi,
    /// satis kaydini isletme sahibi yurutur.
    /// </summary>
    private sealed class PackageSut
    {
        public PurchasePackageCommandHandler Purchase = null!;
        public ReservePackageSessionCommandHandler Reserve = null!;
        public ConsumePackageSessionCommandHandler Consume = null!;
        public ReleasePackageSessionCommandHandler Release = null!;
        public GetCustomerPackageBalanceQueryHandler Balance = null!;

        public CustomerPackage CustomerPackage = null!;
        public FakeRepository<CustomerPackage> CustomerPackages = null!;
        public FakeRepository<PackageUsage> Usages = null!;
        public StubDateTimeService DateTime = null!;
    }

    private static ServicePackage NewPackage(int sessions = 8)
    {
        var package = new ServicePackage("Test Paketi", 500m, 30, BusinessId);
        package.AddItem(new ServicePackageItem(package.Id, ServiceId, sessions));
        return package;
    }

    private static Appointment NewAppointment(
        Guid id,
        EnumAppointmentStatus status = EnumAppointmentStatus.Pending,
        DateTime? appointmentDate = null)
        => TestData.NewAppointment(
            id,
            CustomerId,
            BusinessId,
            ServiceId,
            EmployeeId,
            appointmentDate ?? NowUtc.AddDays(1),
            status);

    private static PackageSut CreateSut(
        IEnumerable<Appointment>? appointments = null,
        int sessions = 8)
    {
        var business = TestData.NewBusiness(OwnerId, BusinessId);
        var package = NewPackage(sessions);
        var customerPackage = new CustomerPackage(BusinessId, CustomerId, package.Id, package.ValidityDays, NowUtc);

        var servicePackages = new FakeRepository<ServicePackage>(new[] { package });
        var customerPackages = new FakeRepository<CustomerPackage>(new[] { customerPackage });
        var usages = new FakeRepository<PackageUsage>();
        var appointmentRepo = new FakeRepository<Appointment>(appointments ?? Array.Empty<Appointment>());
        var businesses = new FakeRepository<Business>(new[] { business });
        var users = new FakeRepository<User>(new[] { TestData.NewUser(CustomerId) });

        var customerUser = TestCurrentUserService.For(CustomerId);
        var ownerUser = TestCurrentUserService.For(OwnerId);
        var dateTime = new StubDateTimeService(NowUtc);

        return new PackageSut
        {
            Purchase = new PurchasePackageCommandHandler(
                servicePackages, customerPackages, businesses, users,
                new FakeUnitOfWork(), ownerUser, dateTime),
            Reserve = new ReservePackageSessionCommandHandler(
                customerPackages, servicePackages, usages, appointmentRepo, businesses,
                new FakeUnitOfWork(), customerUser, dateTime),
            Consume = new ConsumePackageSessionCommandHandler(
                customerPackages, usages, appointmentRepo, businesses,
                new FakeUnitOfWork(), customerUser, dateTime),
            Release = new ReleasePackageSessionCommandHandler(
                usages, customerPackages, businesses,
                new FakeUnitOfWork(), customerUser, dateTime),
            Balance = new GetCustomerPackageBalanceQueryHandler(
                customerPackages, servicePackages, usages, businesses, customerUser),

            CustomerPackage = customerPackage,
            CustomerPackages = customerPackages,
            Usages = usages,
            DateTime = dateTime
        };
    }

    [Fact]
    public async Task Balance_ShouldTrackReserveConsumeRelease()
    {
        // 8 seanslik paket: 6 rezervasyon -> 3 tuketim -> 3 serbest birakma.
        var appointmentIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList();
        var appointments = appointmentIds
            .Select(id => NewAppointment(id))
            .ToList();

        var sut = CreateSut(appointments);

        // 1. Alti randevu icin hak ayir: 8 - 6 = 2 kalan.
        foreach (var id in appointmentIds)
        {
            var result = await sut.Reserve.Handle(
                new ReservePackageSessionCommand
                {
                    CustomerPackageId = sut.CustomerPackage.Id,
                    ServiceId = ServiceId,
                    AppointmentId = id
                },
                CancellationToken.None);

            Assert.True(result.IsSuccess, result.Error?.Message);
        }

        var balance = await sut.Balance.Handle(
            new GetCustomerPackageBalanceQuery { CustomerPackageId = sut.CustomerPackage.Id },
            CancellationToken.None);

        Assert.True(balance.IsSuccess);
        Assert.Equal(8, balance.Value!.PurchasedTotal);
        Assert.Equal(6, balance.Value.ActiveReservations);
        Assert.Equal(0, balance.Value.Consumed);
        Assert.Equal(2, balance.Value.Remaining);

        // 2. Uc randevuyu tamamla ve tuket: aktif rezervasyon 3, tuketilen 3,
        //    kalan hala 2 (3 rezerve + 3 tuket = 6 kullanilan).
        var consumedIds = appointmentIds.Take(3).ToList();
        foreach (var id in consumedIds)
        {
            var appointment = appointments.First(a => a.Id == id);
            appointment.SetStatusForSeed(EnumAppointmentStatus.Completed);
        }

        foreach (var id in consumedIds)
        {
            var result = await sut.Consume.Handle(
                new ConsumePackageSessionCommand
                {
                    CustomerPackageId = sut.CustomerPackage.Id,
                    ServiceId = ServiceId,
                    AppointmentId = id
                },
                CancellationToken.None);

            Assert.True(result.IsSuccess, result.Error?.Message);
        }

        balance = await sut.Balance.Handle(
            new GetCustomerPackageBalanceQuery { CustomerPackageId = sut.CustomerPackage.Id },
            CancellationToken.None);

        Assert.Equal(3, balance.Value!.ActiveReservations);
        Assert.Equal(3, balance.Value.Consumed);
        Assert.Equal(2, balance.Value.Remaining);

        // 3. Kalan 3 aktif rezervasyonu iptal edilen randevular icin
        //    serbest birak: kalan 8 - 3 tuketim = 5.
        foreach (var id in appointmentIds.Skip(3))
        {
            var result = await sut.Release.Handle(
                new ReleasePackageSessionCommand { AppointmentId = id },
                CancellationToken.None);

            Assert.True(result.IsSuccess, result.Error?.Message);
        }

        balance = await sut.Balance.Handle(
            new GetCustomerPackageBalanceQuery { CustomerPackageId = sut.CustomerPackage.Id },
            CancellationToken.None);

        Assert.Equal(0, balance.Value!.ActiveReservations);
        Assert.Equal(3, balance.Value.Consumed);
        Assert.Equal(5, balance.Value.Remaining);
    }

    [Fact]
    public async Task Reserve_ShouldFail_WhenSameAppointmentReservedTwice()
    {
        var appointmentId = Guid.NewGuid();
        var sut = CreateSut(new[] { NewAppointment(appointmentId) });

        var first = await sut.Reserve.Handle(
            new ReservePackageSessionCommand
            {
                CustomerPackageId = sut.CustomerPackage.Id,
                ServiceId = ServiceId,
                AppointmentId = appointmentId
            },
            CancellationToken.None);

        var second = await sut.Reserve.Handle(
            new ReservePackageSessionCommand
            {
                CustomerPackageId = sut.CustomerPackage.Id,
                ServiceId = ServiceId,
                AppointmentId = appointmentId
            },
            CancellationToken.None);

        // Ayni randevuda ayni hakkin iki kez kullanilmasi ve cift islem engellenir.
        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("CONFLICT", second.Error!.Code);
        Assert.Single(sut.Usages.Items);
    }

    [Fact]
    public async Task Reserve_ShouldFail_WhenBalanceExhausted()
    {
        var appointments = Enumerable.Range(0, 9)
            .Select(_ => NewAppointment(Guid.NewGuid()))
            .ToList();

        var sut = CreateSut(appointments);

        foreach (var appointment in appointments.Take(8))
        {
            var result = await sut.Reserve.Handle(
                new ReservePackageSessionCommand
                {
                    CustomerPackageId = sut.CustomerPackage.Id,
                    ServiceId = ServiceId,
                    AppointmentId = appointment.Id
                },
                CancellationToken.None);

            Assert.True(result.IsSuccess, result.Error?.Message);
        }

        var ninth = await sut.Reserve.Handle(
            new ReservePackageSessionCommand
            {
                CustomerPackageId = sut.CustomerPackage.Id,
                ServiceId = ServiceId,
                AppointmentId = appointments[8].Id
            },
            CancellationToken.None);

        // 8 seanslik pakette 9. rezervasyon kabul edilmez.
        Assert.True(ninth.IsFailure);
        Assert.Equal("CONFLICT", ninth.Error!.Code);
        Assert.Equal(8, sut.Usages.Items.Count);
    }

    [Fact]
    public async Task Consume_ShouldFail_WhenAppointmentNotCompleted()
    {
        var appointmentId = Guid.NewGuid();
        var sut = CreateSut(new[] { NewAppointment(appointmentId, EnumAppointmentStatus.Confirmed) });

        var reserveResult = await sut.Reserve.Handle(
            new ReservePackageSessionCommand
            {
                CustomerPackageId = sut.CustomerPackage.Id,
                ServiceId = ServiceId,
                AppointmentId = appointmentId
            },
            CancellationToken.None);

        var consumeResult = await sut.Consume.Handle(
            new ConsumePackageSessionCommand
            {
                CustomerPackageId = sut.CustomerPackage.Id,
                ServiceId = ServiceId,
                AppointmentId = appointmentId
            },
            CancellationToken.None);

        // Tuketim yalnizca Completed randevular icin gecerlidir.
        Assert.True(reserveResult.IsSuccess);
        Assert.True(consumeResult.IsFailure);
        Assert.Equal("CONFLICT", consumeResult.Error!.Code);

        // Rezervasyon hareketi yerinde kalmaya devam eder.
        var reservation = Assert.Single(sut.Usages.Items);
        Assert.Equal(EnumPackageUsageType.Reservation, reservation.UsageType);
        Assert.Equal(EnumPackageUsageStatus.Active, reservation.Status);
    }

    [Fact]
    public async Task Purchase_ShouldSetExpiry_FromPackageValidityDays()
    {
        var sut = CreateSut();

        var result = await sut.Purchase.Handle(
            new PurchasePackageCommand
            {
                ServicePackageId = sut.CustomerPackage.ServicePackageId,
                CustomerId = CustomerId
            },
            CancellationToken.None);

        // Satisi isletme sahibi kaydeder; gecerlilik suresi simdiki zamandan
        // itibaren ValidityDays gun sonrasidir.
        Assert.True(result.IsSuccess, result.Error?.Message);
        var purchased = sut.CustomerPackages.Items.Single(p => p.Id == result.Value);
        Assert.Equal(NowUtc.AddDays(30), purchased.ExpiresAt);
        Assert.Equal(EnumCustomerPackageStatus.Active, purchased.Status);
    }
}
