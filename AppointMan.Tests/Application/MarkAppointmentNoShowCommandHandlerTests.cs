using AppointMan.Application.Features.Appointments.Commands.MarkAppointmentNoShow;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

/// <summary>
/// No-show isaretleme yetkisi ve zaman kurali: karar hizmeti veren tarafa aittir.
/// </summary>
public class MarkAppointmentNoShowCommandHandlerTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid AppointmentId = Guid.NewGuid();
    private static readonly Guid EmployeeUserId = Guid.NewGuid();

    private static readonly DateTime NowUtc = new(2026, 11, 22, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ShouldMarkNoShow_WhenCallerIsBusinessOwner()
    {
        var (appointment, sut) = CreateSut(OwnerId, NowUtc.AddHours(-2));

        var result = await sut.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EnumAppointmentStatus.NoShow, appointment.Status);
    }

    [Fact]
    public async Task Handle_ShouldMarkNoShow_WhenCallerIsAssignedEmployee()
    {
        var (appointment, sut) = CreateSut(EmployeeUserId, NowUtc.AddHours(-2));

        var result = await sut.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EnumAppointmentStatus.NoShow, appointment.Status);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCallerIsCustomer()
    {
        var (appointment, sut) = CreateSut(CustomerId, NowUtc.AddHours(-2));

        var result = await sut.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnumAppointmentStatus.Confirmed, appointment.Status);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenAppointmentIsInFuture()
    {
        var (appointment, sut) = CreateSut(OwnerId, NowUtc.AddHours(3));

        var result = await sut.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnumAppointmentStatus.Confirmed, appointment.Status);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenAppointmentDoesNotExist()
    {
        var clock = new StubDateTimeService(NowUtc);
        var sut = CreateHandler(
            new FakeRepository<Appointment>(),
            TestCurrentUserService.For(OwnerId),
            clock);

        var result = await sut.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private static MarkAppointmentNoShowCommand NewCommand() => new()
    {
        AppointmentId = AppointmentId,
        UserId = OwnerId,
        ChangedBy = "test"
    };

    private static (Appointment Appointment, MarkAppointmentNoShowCommandHandler Sut) CreateSut(
        Guid callerId,
        DateTime appointmentDate)
    {
        var business = TestData.NewBusiness(OwnerId, BusinessId);

        var employeeUser = TestData.NewUser(EmployeeUserId);
        var employee = TestData.NewEmployee(BusinessId, EmployeeId);
        employee.LinkToUser(employeeUser);

        var appointment = TestData.NewAppointment(
            AppointmentId, CustomerId, BusinessId, ServiceId, EmployeeId,
            appointmentDate, EnumAppointmentStatus.Confirmed);

        // EF yok: yetki kontrolu navigasyonlara bakar, elle baglanir.
        NavigationSetter.LinkAppointment(appointment, business, employee);

        var clock = new StubDateTimeService(NowUtc);
        var sut = CreateHandler(
            new FakeRepository<Appointment>(new[] { appointment }),
            TestCurrentUserService.For(callerId),
            clock);

        return (appointment, sut);
    }

    private static MarkAppointmentNoShowCommandHandler CreateHandler(
        FakeRepository<Appointment> appointments,
        TestCurrentUserService currentUser,
        StubDateTimeService clock)
        => new(appointments, new FakeUnitOfWork(), clock, currentUser);
}
