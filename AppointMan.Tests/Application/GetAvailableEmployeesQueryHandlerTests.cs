using AppointMan.Application.Features.Employees.Queries.GetAvailableEmployees;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

/// <summary>
/// Musait calisan sorgusu: calisanin kendi mesai kaydi yoksa isletme mesaisi
/// esas alinmali; izin, hizmet yetkinligi ve no-show/iptal randevular hesaba katilmali.
/// </summary>
public class GetAvailableEmployeesQueryHandlerTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    // 2026-11-23 Pazartesi. Yerel saat = UTC + 3.
    private static readonly DateTime NowUtc = new(2026, 11, 23, 5, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SlotUtc = new(2026, 11, 23, 7, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ShouldReturnEmployee_WhenEmployeeHasNoWorkingHours_ButBusinessIsOpen()
    {
        var employee = NewEmployee();
        var sut = CreateSut(employee, Array.Empty<Appointment>());

        var result = await sut.Handle(NewQuery(SlotUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(EmployeeId, result.Value[0].Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenBusinessIsClosedAtRequestedTime()
    {
        var employee = NewEmployee();
        var sut = CreateSut(employee, Array.Empty<Appointment>());

        // 21:00 yerel saat: isletme mesaisi disinda.
        var closedSlot = new DateTime(2026, 11, 23, 18, 0, 0, DateTimeKind.Utc);
        var result = await sut.Handle(NewQuery(closedSlot), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmployee_WhenOwnWorkingHoursCoverTheSlot()
    {
        var employee = NewEmployee();
        TestData.AddEmployeeWorkingHour(employee, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18));
        var sut = CreateSut(employee, Array.Empty<Appointment>());

        var result = await sut.Handle(NewQuery(SlotUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenEmployeeIsOnApprovedTimeOff()
    {
        var employee = NewEmployee();
        var timeOff = new EmployeeTimeOff(
            employee.Id,
            new DateTime(2026, 11, 23, 9, 0, 0),
            new DateTime(2026, 11, 23, 11, 0, 0),
            EnumTimeOffType.Vacation);
        timeOff.Approve("owner");
        employee.AddTimeOff(timeOff);

        var sut = CreateSut(employee, Array.Empty<Appointment>());

        var result = await sut.Handle(NewQuery(SlotUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenEmployeeCannotProvideService()
    {
        var employee = NewEmployee(addService: false);
        var sut = CreateSut(employee, Array.Empty<Appointment>());

        var result = await sut.Handle(NewQuery(SlotUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmployee_WhenExistingAppointmentIsNoShow()
    {
        var employee = NewEmployee();
        var appointment = TestData.NewAppointment(
            Guid.NewGuid(), CustomerId, BusinessId, ServiceId, EmployeeId,
            SlotUtc, EnumAppointmentStatus.NoShow);

        var sut = CreateSut(employee, new[] { appointment });

        var result = await sut.Handle(NewQuery(SlotUtc), CancellationToken.None);

        // No-show randevu saati isgal etmez.
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenEmployeeHasConfirmedAppointmentAtTheSlot()
    {
        var employee = NewEmployee();
        var appointment = TestData.NewAppointment(
            Guid.NewGuid(), CustomerId, BusinessId, ServiceId, EmployeeId,
            SlotUtc, EnumAppointmentStatus.Confirmed);

        var service = TestData.NewService(BusinessId, ServiceId, 30);

        // EF yok: cakisma penceresi mevcut randevunun servis suresini okur.
        NavigationSetter.SetService(appointment, service);

        var sut = CreateSut(employee, new[] { appointment }, service);

        var result = await sut.Handle(NewQuery(SlotUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenServiceDoesNotExist()
    {
        var employee = NewEmployee();
        var sut = CreateSut(employee, Array.Empty<Appointment>());

        var query = new GetAvailableEmployeesQuery
        {
            BusinessId = BusinessId,
            ServiceId = Guid.NewGuid(),
            AppointmentDate = SlotUtc
        };

        var result = await sut.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private static GetAvailableEmployeesQuery NewQuery(DateTime appointmentDateUtc) => new()
    {
        BusinessId = BusinessId,
        ServiceId = ServiceId,
        AppointmentDate = appointmentDateUtc
    };

    private static Employee NewEmployee(bool addService = true)
    {
        var employee = TestData.NewEmployee(BusinessId, EmployeeId);

        if (addService)
            TestData.AddEmployeeService(employee, ServiceId);

        return employee;
    }

    private static GetAvailableEmployeesQueryHandler CreateSut(
        Employee employee,
        IEnumerable<Appointment> appointments,
        Service? service = null)
    {
        var business = TestData.NewBusiness(OwnerId, BusinessId);
        TestData.AddBusinessWorkingHour(business, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18));

        return new GetAvailableEmployeesQueryHandler(
            new FakeRepository<Employee>(new[] { employee }),
            new FakeRepository<Appointment>(appointments),
            new FakeRepository<Service>(new[] { service ?? TestData.NewService(BusinessId, ServiceId, 30) }),
            new FakeRepository<Business>(new[] { business }),
            new StubDateTimeService(NowUtc));
    }
}
