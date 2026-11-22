using Rezervise.Application.Common.Diagnostics;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Appointments.Commands.CancelAppointment;
using Rezervise.Application.Features.Employees.Commands.ApproveTimeOff;
using Rezervise.Application.Features.Owner.Appointments.Commands.CancelMyAppointment;
using Rezervise.Application.Features.Owner.Appointments.Queries.GetMyAppointments;
using Rezervise.Application.Features.Owner.Businesses.Commands.UpdateMyBusiness;
using Rezervise.Application.Features.Owner.Businesses.Queries.GetMyBusinessDetail;
using Rezervise.Application.Features.Owner.Businesses.Queries.GetMyBusinesses;
using Rezervise.Application.Features.Owner.Employees.Commands.ApproveMyTimeOff;
using Rezervise.Application.Features.Owner.Finance.Queries.GetMySales;
using Rezervise.Application.Features.Owner.Stock.Queries.GetMyPackages;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests.Fakes;
using MediatR;

namespace Rezervise.Tests.Application.BackOffice;

/// <summary>
/// Isletme sahibi (BusinessOwner) portalinin backoffice testleri.
/// Owner uc'lari tenant-scoped'dir: her test sahiplik dogrulamasini,
/// IMediator sarmalama desenini ve denetim kaydini kontrol eder.
/// </summary>
public class OwnerTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid OtherOwnerId = Guid.NewGuid();
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OtherBusinessId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid OtherEmployeeId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();

    private static readonly DateTime NowUtc = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    // ===== Denetim kaydi fake'leri (AdminAppointmentTests deseninden) =====

    /// <summary>Denetim kaydinin yakalandigi bellek ici kayit.</summary>
    private sealed class AuditEntry
    {
        public Guid ActorUserId { get; set; }
        public string? ActorRole { get; set; }
        public Guid? BusinessId { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Action { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? Reason { get; set; }
        public string? CorrelationId { get; set; }
        public string? IpAddress { get; set; }
    }

    /// <summary>IAdminAuditSink fake'i: kayitlari listeye alir, hata uretmez.</summary>
    private sealed class FakeAdminAuditSink : IAdminAuditSink
    {
        public List<AuditEntry> Entries { get; } = new();

        public Task RecordAsync(
            Guid actorUserId,
            string actorRole,
            Guid? businessId,
            string entityType,
            string entityId,
            string action,
            string? oldValues = null,
            string? newValues = null,
            string? reason = null,
            string? correlationId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            Entries.Add(new AuditEntry
            {
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                BusinessId = businessId,
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                Reason = reason,
                CorrelationId = correlationId,
                IpAddress = ipAddress
            });

            return Task.CompletedTask;
        }
    }

    /// <summary>Sabit correlation ve IP ureten denetim baglami fake'i.</summary>
    private sealed class FakeAdminAuditContext : IAdminAuditContext
    {
        public string CorrelationId => "test-correlation";
        public string IpAddress => "127.0.0.1";
    }

    /// <summary>
    /// IMediator fake'i: mock kutuphanesi yerine verilen delegate'i
    /// calistirir. Delegate, gercek domain handler'i sarmalar; boylece
    /// owner islemleri domain akisini dogru sekilde tetikler.
    /// </summary>
    private sealed class FakeMediator : IMediator
    {
        private readonly Func<object, Task<object>> _send;

        public FakeMediator(Func<object, Task<object>> send)
        {
            _send = send;
        }

        /// <summary>Delegate'e gonderilen istekler; dogrulama icin tutulur.</summary>
        public List<object> SentRequests { get; } = new();

        public async Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return (TResponse)await _send(request);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => throw new NotSupportedException("Yanitsiz istekler bu testte kullanilmaz.");

        public async Task<object> Send(object request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return await _send(request);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Stream istekleri bu testte kullanilmaz.");

        public IAsyncEnumerable<object> CreateStream(
            object request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Stream istekleri bu testte kullanilmaz.");

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.CompletedTask;
    }

    // ===== Yardimcilar =====

    /// <summary>
    /// Sahip kimligi. Randevu iptali testinde ek olarak "Admin" rolu
    /// verilir: gercek domain handler musteri/isletme ayrimini yuklenmis
    /// nav property'ler uzerinden yapar, fake repository includes
    /// yuklemediginden yonetici kisayolu test ortaminda gerekir.
    /// Production'da roller JWT claim'lerinden zaten tam gelir.
    /// </summary>
    private static TestCurrentUserService OwnerUser(params string[] extraRoles)
        => TestCurrentUserService.For(OwnerId, new[] { "BusinessOwner" }.Concat(extraRoles).ToArray());

    private static TestCurrentUserService AnonymousUser()
        => new() { UserId = null, IsAuthenticated = false };

    private static (FakeAdminAuditSink Sink, FakeAdminAuditContext Context) CreateAudit()
        => (new FakeAdminAuditSink(), new FakeAdminAuditContext());

    private static FakeRepository<Business> OwnedBusinesses()
        => new(new[]
        {
            TestData.NewBusiness(OwnerId, BusinessId),
            TestData.NewBusiness(OtherOwnerId, OtherBusinessId)
        });

    private static Appointment NewAppointment(
        Guid id,
        Guid businessId,
        DateTime appointmentDate,
        EnumAppointmentStatus status = EnumAppointmentStatus.Confirmed)
        => TestData.NewAppointment(id, CustomerId, businessId, ServiceId, EmployeeId, appointmentDate, status);

    /// <summary>
    /// Owner iptalini, gercek CancelAppointmentCommandHandler'i saran
    /// FakeMediator ile kurar. Domain kontrolu tek kaynaktan yurur.
    /// </summary>
    private static (CancelMyAppointmentCommandHandler Handler, FakeMediator Mediator) CreateCancelSut(
        FakeRepository<Business> businessRepo,
        FakeRepository<Appointment> appointmentRepo,
        TestCurrentUserService currentUser,
        FakeAdminAuditSink sink,
        FakeAdminAuditContext context)
    {
        var innerHandler = new CancelAppointmentCommandHandler(
            appointmentRepo,
            new StubDateTimeService(NowUtc),
            new FakeUnitOfWork(),
            new RezerviseMetrics(),
            currentUser);

        var mediator = new FakeMediator(async request =>
        {
            if (request is CancelAppointmentCommand command)
            {
                var innerResult = await innerHandler.Handle(command, CancellationToken.None);
                return (object)innerResult;
            }

            throw new NotSupportedException($"Beklenmeyen istek tipi: {request.GetType().Name}");
        });

        var handler = new CancelMyAppointmentCommandHandler(
            businessRepo,
            appointmentRepo,
            mediator,
            currentUser,
            sink,
            context);

        return (handler, mediator);
    }

    /// <summary>
    /// Owner izin onayini, gercek ApproveTimeOffCommandHandler'i saran
    /// FakeMediator ile kurar. Cakisma kurallari domain'de yurur.
    /// </summary>
    private static (ApproveMyTimeOffCommandHandler Handler, FakeMediator Mediator) CreateApproveSut(
        FakeRepository<Business> businessRepo,
        FakeRepository<Employee> employeeRepo,
        FakeRepository<EmployeeTimeOff> timeOffRepo,
        TestCurrentUserService currentUser,
        FakeAdminAuditSink sink,
        FakeAdminAuditContext context)
    {
        var innerHandler = new ApproveTimeOffCommandHandler(
            timeOffRepo,
            employeeRepo,
            new FakeRepository<Appointment>(),
            businessRepo,
            new FakeUnitOfWork(),
            currentUser);

        var mediator = new FakeMediator(async request =>
        {
            if (request is ApproveTimeOffCommand command)
            {
                var innerResult = await innerHandler.Handle(command, CancellationToken.None);
                return (object)innerResult;
            }

            throw new NotSupportedException($"Beklenmeyen istek tipi: {request.GetType().Name}");
        });

        var handler = new ApproveMyTimeOffCommandHandler(
            businessRepo,
            employeeRepo,
            timeOffRepo,
            mediator,
            currentUser,
            sink,
            context);

        return (handler, mediator);
    }

    // ===== Sahiplik =====

    [Fact]
    public async Task GetMyBusinessDetail_WhenBusinessOwnedBySomeoneElse_ShouldReturnForbidden()
    {
        var sut = new GetMyBusinessDetailQueryHandler(
            OwnedBusinesses(),
            new FakeRepository<Employee>(),
            new FakeRepository<Service>(),
            new FakeRepository<Appointment>(),
            OwnerUser());

        var result = await sut.Handle(
            new GetMyBusinessDetailQuery { BusinessId = OtherBusinessId },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Equal("Bu isletme size ait degil.", result.Error.Message);
    }

    [Fact]
    public async Task GetMyBusinessDetail_WhenUserIdMissing_ShouldReturnUnauthorized()
    {
        var sut = new GetMyBusinessDetailQueryHandler(
            new FakeRepository<Business>(),
            new FakeRepository<Employee>(),
            new FakeRepository<Service>(),
            new FakeRepository<Appointment>(),
            AnonymousUser());

        var result = await sut.Handle(
            new GetMyBusinessDetailQuery { BusinessId = BusinessId },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    // ===== Isletme listesi =====

    [Fact]
    public async Task GetMyBusinesses_ShouldReturnOnlyOwnedBusinesses()
    {
        var extraOwnedId = Guid.NewGuid();
        var businesses = new List<Business>
        {
            TestData.NewBusiness(OwnerId, BusinessId),
            TestData.NewBusiness(OwnerId, extraOwnedId),
            TestData.NewBusiness(OtherOwnerId, OtherBusinessId)
        };

        var sut = new GetMyBusinessesQueryHandler(
            new FakeRepository<Business>(businesses),
            OwnerUser());

        var result = await sut.Handle(new GetMyBusinessesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(
            new[] { BusinessId, extraOwnedId }.OrderBy(id => id),
            result.Value.Items.Select(b => b.Id).OrderBy(id => id));
    }

    // ===== Isletme guncelleme =====

    [Fact]
    public async Task UpdateMyBusiness_ShouldRenameAndRecordAudit()
    {
        var business = TestData.NewBusiness(OwnerId, BusinessId);
        var businessRepo = new FakeRepository<Business>(new[] { business });

        var (sink, context) = CreateAudit();
        var sut = new UpdateMyBusinessCommandHandler(
            businessRepo,
            new FakeUnitOfWork(),
            OwnerUser(),
            sink,
            context);

        var result = await sut.Handle(
            new UpdateMyBusinessCommand
            {
                BusinessId = BusinessId,
                Name = "Yeni Isletme Adi",
                Reason = "Tanim guncellemesi"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BusinessId, result.Value);
        Assert.Equal("Yeni Isletme Adi", business.Name);

        // Basari sonrasi denetim kaydi yazilmali.
        var entry = Assert.Single(sink.Entries);
        Assert.Equal("OwnerUpdateBusiness", entry.Action);
        Assert.Equal("Business", entry.EntityType);
        Assert.Equal(BusinessId, entry.BusinessId);
        Assert.Equal(OwnerId, entry.ActorUserId);
        Assert.Equal("Tanim guncellemesi", entry.Reason);
        Assert.Contains("Test Isletmesi", entry.OldValues, StringComparison.Ordinal);
        Assert.Contains("Yeni Isletme Adi", entry.NewValues, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateMyBusiness_WithoutAnyField_ShouldReturnValidationError()
    {
        var (sink, context) = CreateAudit();
        var sut = new UpdateMyBusinessCommandHandler(
            OwnedBusinesses(),
            new FakeUnitOfWork(),
            OwnerUser(),
            sink,
            context);

        var result = await sut.Handle(
            new UpdateMyBusinessCommand { BusinessId = BusinessId },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        Assert.Empty(sink.Entries);
    }

    // ===== Randevu listesi =====

    [Fact]
    public async Task GetMyAppointments_ShouldFilterByBusinessAndStatus()
    {
        var tomorrow = NowUtc.Date.AddDays(1);

        var a1 = NewAppointment(Guid.NewGuid(), BusinessId, tomorrow.AddHours(10), EnumAppointmentStatus.Confirmed);
        var a2 = NewAppointment(Guid.NewGuid(), BusinessId, tomorrow.AddHours(11), EnumAppointmentStatus.Pending);
        var a3 = NewAppointment(Guid.NewGuid(), OtherBusinessId, tomorrow.AddHours(12), EnumAppointmentStatus.Confirmed);

        var sut = new GetMyAppointmentsQueryHandler(
            OwnedBusinesses(),
            new FakeRepository<Appointment>(new[] { a1, a2, a3 }),
            OwnerUser());

        var result = await sut.Handle(
            new GetMyAppointmentsQuery
            {
                BusinessId = BusinessId,
                Status = EnumAppointmentStatus.Confirmed
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(a1.Id, item.Id);

        // Durum filtresi olmadan isletmenin tum randevulari.
        var all = await sut.Handle(
            new GetMyAppointmentsQuery { BusinessId = BusinessId },
            CancellationToken.None);

        Assert.Equal(2, all.Value!.TotalCount);
    }

    // ===== Randevu iptali (domain sarmalamasi) =====

    [Fact]
    public async Task CancelMyAppointment_ShouldDelegateToDomainHandlerAndRecordAudit()
    {
        var appointmentId = Guid.NewGuid();
        var appointment = NewAppointment(appointmentId, BusinessId, NowUtc.AddHours(3));
        var appointmentRepo = new FakeRepository<Appointment>(new[] { appointment });

        var (sink, context) = CreateAudit();
        var currentUser = OwnerUser("Admin");
        var (sut, mediator) = CreateCancelSut(OwnedBusinesses(), appointmentRepo, currentUser, sink, context);

        var result = await sut.Handle(
            new CancelMyAppointmentCommand
            {
                BusinessId = BusinessId,
                AppointmentId = appointmentId,
                Reason = "Calisan izninde",
                CancellationReason = "Isletme tarafindan iptal edildi"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(appointmentId, result.Value);
        Assert.Equal(EnumAppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal("Isletme tarafindan iptal edildi", appointment.CancellationReason);

        // Domain komutu dogru parametrelerle mediator'a gitmeli.
        var sentCommand = Assert.IsType<CancelAppointmentCommand>(Assert.Single(mediator.SentRequests));
        Assert.Equal(appointmentId, sentCommand.AppointmentId);
        Assert.Equal(CustomerId, sentCommand.UserId);
        Assert.Equal("Isletme tarafindan iptal edildi", sentCommand.Reason);
        Assert.Equal($"Owner:{OwnerId}", sentCommand.CancelledBy);

        // Basari sonrasi denetim kaydi yazilmali.
        var entry = Assert.Single(sink.Entries);
        Assert.Equal("OwnerCancelAppointment", entry.Action);
        Assert.Equal("Appointment", entry.EntityType);
        Assert.Equal(BusinessId, entry.BusinessId);
        Assert.Equal(OwnerId, entry.ActorUserId);
        Assert.Equal("Calisan izninde", entry.Reason);
        Assert.Contains("Confirmed", entry.OldValues, StringComparison.Ordinal);
        Assert.Contains("Cancelled", entry.NewValues, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancelMyAppointment_WhenAppointmentInDifferentBusiness_ShouldReturnForbidden()
    {
        // Randevu baska isletmenin: sahiplik isletmeden gecer ama randevu
        // istenen isletmeye ait degil.
        var appointmentId = Guid.NewGuid();
        var appointment = NewAppointment(appointmentId, OtherBusinessId, NowUtc.AddHours(3));
        var appointmentRepo = new FakeRepository<Appointment>(new[] { appointment });

        var (sink, context) = CreateAudit();
        var (sut, mediator) = CreateCancelSut(OwnedBusinesses(), appointmentRepo, OwnerUser("Admin"), sink, context);

        var result = await sut.Handle(
            new CancelMyAppointmentCommand
            {
                BusinessId = BusinessId,
                AppointmentId = appointmentId,
                Reason = "Calisan izninde"
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Equal(EnumAppointmentStatus.Confirmed, appointment.Status);
        Assert.Empty(mediator.SentRequests);
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task CancelMyAppointment_WithEmptyReason_ShouldReturnValidationError()
    {
        var appointmentId = Guid.NewGuid();
        var appointment = NewAppointment(appointmentId, BusinessId, NowUtc.AddHours(3));
        var appointmentRepo = new FakeRepository<Appointment>(new[] { appointment });

        var (sink, context) = CreateAudit();
        var (sut, mediator) = CreateCancelSut(OwnedBusinesses(), appointmentRepo, OwnerUser("Admin"), sink, context);

        var result = await sut.Handle(
            new CancelMyAppointmentCommand
            {
                BusinessId = BusinessId,
                AppointmentId = appointmentId,
                Reason = "   "
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        // Gerekce olmadigindan domain'e hicbir istek gitmez.
        Assert.Empty(mediator.SentRequests);
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task CancelMyAppointment_WhenBusinessNotOwned_ShouldReturnForbidden()
    {
        var appointmentId = Guid.NewGuid();
        var appointment = NewAppointment(appointmentId, OtherBusinessId, NowUtc.AddHours(3));
        var appointmentRepo = new FakeRepository<Appointment>(new[] { appointment });

        var (sink, context) = CreateAudit();
        var (sut, mediator) = CreateCancelSut(OwnedBusinesses(), appointmentRepo, OwnerUser("Admin"), sink, context);

        // Randevu dogru isletmede ama isletme baskasinin: sahiplik
        // kontrolu diger tum adimlardan once reddetmeli.
        var result = await sut.Handle(
            new CancelMyAppointmentCommand
            {
                BusinessId = OtherBusinessId,
                AppointmentId = appointmentId,
                Reason = "Yetki siniri testi"
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Equal(EnumAppointmentStatus.Confirmed, appointment.Status);
        Assert.Empty(mediator.SentRequests);
        Assert.Empty(sink.Entries);
    }

    // ===== Izin onayi (domain sarmalamasi) =====

    [Fact]
    public async Task ApproveMyTimeOff_ShouldDelegateToDomainHandlerAndRecordAudit()
    {
        var timeOffId = Guid.NewGuid();
        var employee = TestData.NewEmployee(BusinessId, EmployeeId);
        var timeOff = new EmployeeTimeOff(
            timeOffId,
            EmployeeId,
            NowUtc.AddDays(5),
            NowUtc.AddDays(6),
            EnumTimeOffType.Vacation,
            "Yillik izin");
        timeOff.ClearDomainEvents();

        var businessRepo = OwnedBusinesses();
        var employeeRepo = new FakeRepository<Employee>(new[] { employee });
        var timeOffRepo = new FakeRepository<EmployeeTimeOff>(new[] { timeOff });

        var (sink, context) = CreateAudit();
        var currentUser = OwnerUser();
        var (sut, mediator) = CreateApproveSut(businessRepo, employeeRepo, timeOffRepo, currentUser, sink, context);

        var result = await sut.Handle(
            new ApproveMyTimeOffCommand
            {
                BusinessId = BusinessId,
                TimeOffId = timeOffId,
                Reason = "Uygun goruldu"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(timeOffId, result.Value);
        Assert.True(timeOff.IsApproved);

        // Domain komutu dogru parametrelerle mediator'a gitmeli.
        var sentCommand = Assert.IsType<ApproveTimeOffCommand>(Assert.Single(mediator.SentRequests));
        Assert.Equal(timeOffId, sentCommand.TimeOffId);
        Assert.Equal(OwnerId, sentCommand.RequesterId);

        // Basari sonrasi denetim kaydi yazilmali.
        var entry = Assert.Single(sink.Entries);
        Assert.Equal("OwnerApproveTimeOff", entry.Action);
        Assert.Equal("EmployeeTimeOff", entry.EntityType);
        Assert.Equal(BusinessId, entry.BusinessId);
        Assert.Equal(OwnerId, entry.ActorUserId);
        Assert.Equal("Uygun goruldu", entry.Reason);
        Assert.Contains("Pending", entry.OldValues, StringComparison.Ordinal);
        Assert.Contains("Approved", entry.NewValues, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApproveMyTimeOff_WhenEmployeeInDifferentBusiness_ShouldReturnForbidden()
    {
        // Izin baska isletmenin calisanina ait: kapsam dogrulamasi
        // mediator'a hicbir istek gondermeden reddetmeli.
        var timeOffId = Guid.NewGuid();
        var otherEmployee = TestData.NewEmployee(OtherBusinessId, OtherEmployeeId);
        var timeOff = new EmployeeTimeOff(
            timeOffId,
            OtherEmployeeId,
            NowUtc.AddDays(5),
            NowUtc.AddDays(6),
            EnumTimeOffType.Vacation,
            "Baska isletme izni");
        timeOff.ClearDomainEvents();

        var businessRepo = OwnedBusinesses();
        var employeeRepo = new FakeRepository<Employee>(new[] { otherEmployee });
        var timeOffRepo = new FakeRepository<EmployeeTimeOff>(new[] { timeOff });

        var (sink, context) = CreateAudit();
        var (sut, mediator) = CreateApproveSut(businessRepo, employeeRepo, timeOffRepo, OwnerUser(), sink, context);

        var result = await sut.Handle(
            new ApproveMyTimeOffCommand
            {
                BusinessId = BusinessId,
                TimeOffId = timeOffId
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.False(timeOff.IsApproved);
        Assert.Empty(mediator.SentRequests);
        Assert.Empty(sink.Entries);
    }

    // ===== Finans =====

    [Fact]
    public async Task GetMySales_ShouldFilterByDateAndSumGrandTotal()
    {
        var from = NowUtc.Date.AddDays(-1);
        var to = NowUtc.Date.AddDays(2);

        var s1 = NewSale(Guid.NewGuid(), BusinessId, NowUtc.Date.AddHours(10), 100m);
        var s2 = NewSale(Guid.NewGuid(), BusinessId, NowUtc.Date.AddHours(11), 250m);

        // Aralik disi: tarih filtresinde disarida kalmali.
        var s3 = NewSale(Guid.NewGuid(), BusinessId, NowUtc.Date.AddDays(-10), 75m);

        // Baska isletmenin satisi: kapsam disi.
        var s4 = NewSale(Guid.NewGuid(), OtherBusinessId, NowUtc.Date.AddHours(12), 500m);

        var sut = new GetMySalesQueryHandler(
            OwnedBusinesses(),
            new FakeRepository<Sale>(new[] { s1, s2, s3, s4 }),
            OwnerUser());

        var result = await sut.Handle(
            new GetMySalesQuery { BusinessId = BusinessId, FromDate = from, ToDate = to },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(350m, result.Value.TotalGrandTotal);
    }

    // ===== Musteri paketleri =====

    [Fact]
    public async Task GetMyPackages_ShouldComputeRemainingFromUsages()
    {
        // Paket tanimi: 5 + 3 = 8 seans.
        var packageId = Guid.NewGuid();
        var servicePackage = new ServicePackage(packageId, "8 Seans Paketi", 800m, 90, BusinessId);
        servicePackage.AddItem(new ServicePackageItem(packageId, ServiceId, 5));
        servicePackage.AddItem(new ServicePackageItem(packageId, Guid.NewGuid(), 3));
        servicePackage.ClearDomainEvents();

        var customerPackage = new CustomerPackage(
            BusinessId, CustomerId, packageId, validityDays: 90, NowUtc);
        customerPackage.ClearDomainEvents();

        var packageIdOfUsage = customerPackage.Id;

        // 2 tuketim + 1 rezervasyon: kalan 8 - 2 - 1 = 5.
        var usages = new List<PackageUsage>
        {
            PackageUsage.Consume(packageIdOfUsage, ServiceId, Guid.NewGuid(), NowUtc),
            PackageUsage.Consume(packageIdOfUsage, ServiceId, Guid.NewGuid(), NowUtc),
            PackageUsage.Reserve(packageIdOfUsage, ServiceId, Guid.NewGuid(), NowUtc)
        };

        var sut = new GetMyPackagesQueryHandler(
            OwnedBusinesses(),
            new FakeRepository<CustomerPackage>(new[] { customerPackage }),
            new FakeRepository<ServicePackage>(new[] { servicePackage }),
            new FakeRepository<PackageUsage>(usages),
            OwnerUser());

        var result = await sut.Handle(
            new GetMyPackagesQuery { BusinessId = BusinessId },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(8, item.Purchased);
        Assert.Equal(2, item.Consumed);
        Assert.Equal(1, item.Reserved);
        Assert.Equal(5, item.Remaining);
    }

    /// <summary>Tek satirli satis uretir; GrandTotal = quantity * unitPrice.</summary>
    private static Sale NewSale(Guid id, Guid businessId, DateTime soldAt, decimal unitPrice)
    {
        var sale = new Sale(id, businessId, soldAt, "TRY");
        sale.AddLine(new SaleLine(
            id,
            EnumSaleItemType.Service,
            ServiceId,
            productId: null,
            quantity: 1,
            unitPrice,
            discountAmount: 0,
            taxAmount: 0,
            EmployeeId));
        sale.ClearDomainEvents();
        return sale;
    }
}
