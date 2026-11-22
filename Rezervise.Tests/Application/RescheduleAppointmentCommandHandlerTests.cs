using Rezervise.Application.Common.Diagnostics;
using Rezervise.Application.Features.Appointments.Commands.RescheduleAppointment;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

public class RescheduleAppointmentCommandHandlerTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid OldEmployeeId = Guid.NewGuid();
    private static readonly Guid NewEmployeeId = Guid.NewGuid();
    private static readonly Guid AppointmentId = Guid.NewGuid();

    private static readonly DateTime NowUtc = new(2026, 11, 22, 20, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OldSlotUtc = new(2026, 11, 23, 7, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime NewSlotUtc = new(2026, 11, 23, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ShouldPermanentlyAssignNewEmployee()
    {
        var appointment = CreateAppointment();
        var clock = new StubDateTimeService(NowUtc);
        var sut = CreateSut(clock, appointment);

        var result = await sut.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NewEmployeeId, appointment.EmployeeId);
        Assert.Equal(NewSlotUtc, appointment.AppointmentDate);
        Assert.Equal(NewSlotUtc.AddMinutes(30), appointment.EndTime);
    }

    [Fact]
    public async Task Handle_ShouldKeepExistingEmployee_WhenNoNewEmployeeSupplied()
    {
        var appointment = CreateAppointment();
        var clock = new StubDateTimeService(NowUtc);
        var sut = CreateSut(clock, appointment);

        var command = NewCommand();
        command.NewEmployeeId = null;
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(OldEmployeeId, appointment.EmployeeId);
    }

    [Fact]
    public async Task Handle_ShouldRejectDoubleBooking()
    {
        var appointment = CreateAppointment();
        var clock = new StubDateTimeService(NowUtc);
        var guard = new FakeBookingGuard();
        guard.AddConflict(NewEmployeeId, NewSlotUtc, NewSlotUtc.AddMinutes(30));
        var sut = CreateSut(clock, appointment, guard);

        var result = await sut.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        Assert.Equal(OldSlotUtc, appointment.AppointmentDate);
        Assert.Equal(OldEmployeeId, appointment.EmployeeId);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenCallerIsNotAppointmentOwner()
    {
        var appointment = CreateAppointment();
        var clock = new StubDateTimeService(NowUtc);
        var sut = CreateSut(clock, appointment, currentUser: TestCurrentUserService.For(Guid.NewGuid()));

        var result = await sut.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    private static RescheduleAppointmentCommand NewCommand() => new()
    {
        AppointmentId = AppointmentId,
        UserId = CustomerId,
        NewEmployeeId = NewEmployeeId,
        NewAppointmentDate = NewSlotUtc,
        RescheduledBy = "test"
    };

    private static Appointment CreateAppointment()
        => TestData.NewAppointment(
            AppointmentId, CustomerId, BusinessId, ServiceId, OldEmployeeId,
            OldSlotUtc, EnumAppointmentStatus.Pending);

    private static RescheduleAppointmentCommandHandler CreateSut(
        StubDateTimeService clock,
        Appointment appointment,
        FakeBookingGuard? guard = null,
        TestCurrentUserService? currentUser = null)
    {
        var business = TestData.NewBusiness(OwnerId, BusinessId);
        TestData.AddBusinessWorkingHour(business, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18));

        var oldEmployee = CreateEmployee(OldEmployeeId);
        var newEmployee = CreateEmployee(NewEmployeeId);

        return new RescheduleAppointmentCommandHandler(
            new FakeRepository<Appointment>(new[] { appointment }),
            new FakeRepository<Business>(new[] { business }),
            new FakeRepository<Service>(new[] { TestData.NewService(BusinessId, ServiceId, 30) }),
            new FakeRepository<Employee>(new[] { oldEmployee, newEmployee }),
            new FakeRepository<BlacklistEntry>(),
            new FakeUnitOfWork(),
            clock,
            guard ?? new FakeBookingGuard(),
            new RezerviseMetrics(),
            currentUser ?? TestCurrentUserService.For(CustomerId));
    }

    private static Employee CreateEmployee(Guid id)
    {
        var employee = TestData.NewEmployee(BusinessId, id);
        TestData.AddEmployeeService(employee, ServiceId);
        TestData.AddEmployeeWorkingHour(employee, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18));
        return employee;
    }
}
