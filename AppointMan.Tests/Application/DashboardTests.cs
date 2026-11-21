using AppointMan.Application.Features.Reporting.Queries.GetBusinessDashboard;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

/// <summary>
/// Yonetici dashboard gunluk kartlarinin izole testleri (dokuman Modul 15.1).
/// </summary>
public class DashboardTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    // 2026-11-21 bir Cumartesi; calisma saati tanimi o gune bagli.
    private static readonly DateTime Today = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    private static Appointment NewAppointment(
        DateTime date,
        EnumAppointmentStatus status,
        DateTime? endTime = null)
        => TestData.NewAppointment(
            Guid.NewGuid(),
            CustomerId,
            BusinessId,
            ServiceId,
            EmployeeId,
            date,
            status,
            endTime);

    private static GetBusinessDashboardQueryHandler NewSut(
        FakeRepository<Appointment>? appointments = null,
        FakeRepository<WorkingHour>? workingHours = null,
        FakeRepository<Employee>? employees = null)
    {
        return new GetBusinessDashboardQueryHandler(
            new FakeRepository<AppointMan.Domain.Entities.Business>(new[] { TestData.NewBusiness(OwnerId, BusinessId) }),
            appointments ?? new FakeRepository<Appointment>(),
            workingHours ?? new FakeRepository<WorkingHour>(),
            employees ?? new FakeRepository<Employee>(),
            new FakeRepository<Service>(new[] { TestData.NewService(BusinessId, ServiceId) }),
            TestCurrentUserService.For(OwnerId));
    }

    private static GetBusinessDashboardQuery NewQuery() => new()
    {
        BusinessId = BusinessId,
        Date = Today
    };

    [Fact]
    public async Task Handle_GunIcindekiPendingConfirmedSayimi_Dogrudur()
    {
        var appointments = new FakeRepository<Appointment>(new Appointment[]
        {
            // Bugun: 2 Pending + 1 Confirmed aktif.
            NewAppointment(Today.AddHours(1), EnumAppointmentStatus.Pending),
            NewAppointment(Today.AddHours(2), EnumAppointmentStatus.Pending),
            NewAppointment(Today.AddHours(3), EnumAppointmentStatus.Confirmed),
            // Bugun: durum kartlari.
            NewAppointment(Today.AddHours(4), EnumAppointmentStatus.Completed),
            NewAppointment(Today.AddHours(5), EnumAppointmentStatus.Cancelled),
            NewAppointment(Today.AddHours(6), EnumAppointmentStatus.NoShow),
            // Diger gunler: sayilmamali.
            NewAppointment(Today.AddDays(1), EnumAppointmentStatus.Pending),
            NewAppointment(Today.AddDays(-1), EnumAppointmentStatus.Pending)
        });

        var sut = NewSut(appointments);

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.BugunkiRandevular);
        Assert.Equal(1, result.Value.Tamamlanan);
        Assert.Equal(1, result.Value.Iptaller);
        Assert.Equal(1, result.Value.NoShow);
    }

    [Fact]
    public async Task Handle_GunSinirlarindakiRandevular_DogruGuneDusmeli()
    {
        var appointments = new FakeRepository<Appointment>(new Appointment[]
        {
            // Gun basi dahil, gun sonu haric.
            NewAppointment(Today.Date, EnumAppointmentStatus.Pending),
            NewAppointment(Today.Date.AddDays(1).AddTicks(-1), EnumAppointmentStatus.Pending),
            NewAppointment(Today.Date.AddDays(1), EnumAppointmentStatus.Pending)
        });

        var sut = NewSut(appointments);

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.BugunkiRandevular);
    }

    [Fact]
    public async Task Handle_DolulukOrani_CalisanaVeAcikSaateGoreHesaplanir()
    {
        var appointments = new FakeRepository<Appointment>(new Appointment[]
        {
            // 10:00-10:30 Confirmed -> 30 dakika dolu.
            NewAppointment(Today.AddHours(10), EnumAppointmentStatus.Confirmed, endTime: Today.AddHours(10).AddMinutes(30))
        });

        var workingHours = new FakeRepository<WorkingHour>(new[]
        {
            new WorkingHour(DayOfWeek.Saturday, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), BusinessId)
        });

        var employees = new FakeRepository<Employee>(new[]
        {
            TestData.NewEmployee(BusinessId, EmployeeId)
        });

        var sut = NewSut(appointments, workingHours, employees);

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Kapasite: 1 calisan x 480 dakika = 480; dolu: 30 -> 0.06.
        Assert.Equal(0.06m, result.Value.DolulukOrani);
    }

    [Fact]
    public async Task Handle_CalisanYoksa_DolulukSifirdir()
    {
        var appointments = new FakeRepository<Appointment>(new Appointment[]
        {
            NewAppointment(Today.AddHours(10), EnumAppointmentStatus.Confirmed, endTime: Today.AddHours(10).AddMinutes(30))
        });

        var workingHours = new FakeRepository<WorkingHour>(new[]
        {
            new WorkingHour(DayOfWeek.Saturday, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), BusinessId)
        });

        // Calisan yok: kapasite 0 -> doluluk 0.
        var sut = NewSut(appointments, workingHours, employees: new FakeRepository<Employee>());

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.DolulukOrani);
    }

    [Fact]
    public async Task Handle_KapaliGun_DolulukSifirdir()
    {
        var appointments = new FakeRepository<Appointment>(new Appointment[]
        {
            NewAppointment(Today.AddHours(10), EnumAppointmentStatus.Confirmed, endTime: Today.AddHours(10).AddMinutes(30))
        });

        var closedDay = new WorkingHour(DayOfWeek.Saturday, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), BusinessId);
        closedDay.Close();

        var workingHours = new FakeRepository<WorkingHour>(new[] { closedDay });
        var employees = new FakeRepository<Employee>(new[] { TestData.NewEmployee(BusinessId, EmployeeId) });

        var sut = NewSut(appointments, workingHours, employees);

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.DolulukOrani);
    }

    [Fact]
    public async Task Handle_BaskaIsletmeninRandevulari_Sayilmaz()
    {
        var otherBusinessId = Guid.NewGuid();
        var otherBusinessAppointment = TestData.NewAppointment(
            Guid.NewGuid(),
            CustomerId,
            otherBusinessId,
            ServiceId,
            EmployeeId,
            Today.AddHours(1),
            EnumAppointmentStatus.Pending);

        var appointments = new FakeRepository<Appointment>(new[] { otherBusinessAppointment });

        var sut = NewSut(appointments);

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.BugunkiRandevular);
    }

    [Fact]
    public async Task Handle_CallerIsletmeSahibiDegilse_UnauthorizedDoner()
    {
        var sut = NewGetDashboardHandlerForUnauthorized();

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    private static GetBusinessDashboardQueryHandler NewGetDashboardHandlerForUnauthorized()
        => new(
            new FakeRepository<AppointMan.Domain.Entities.Business>(new[] { TestData.NewBusiness(OwnerId, BusinessId) }),
            new FakeRepository<Appointment>(),
            new FakeRepository<WorkingHour>(),
            new FakeRepository<Employee>(),
            new FakeRepository<Service>(new[] { TestData.NewService(BusinessId, ServiceId) }),
            TestCurrentUserService.For(Guid.NewGuid()));
}
