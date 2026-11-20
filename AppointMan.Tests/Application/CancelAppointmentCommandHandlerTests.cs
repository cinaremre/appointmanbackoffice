using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Features.Appointments.Commands.CancelAppointment;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

/// <summary>
/// Iptal akisinda 2 saat kurali: musteri son 2 saatte iptal edemez,
/// isletme sahibi ve atanmis calisan bu sinirlamadan muaftir.
/// </summary>
public class CancelAppointmentCommandHandlerTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid AppointmentId = Guid.NewGuid();

    private static readonly DateTime NowUtc = new(2026, 11, 22, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ShouldAllowCustomer_WhenMoreThanTwoHoursRemain()
    {
        var (appointment, sut) = CreateSut(CustomerId, NowUtc.AddHours(3));

        var result = await sut.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EnumAppointmentStatus.Cancelled, appointment.Status);
    }

    [Fact]
    public async Task Handle_ShouldBlockCustomer_WhenLessThanTwoHoursRemain()
    {
        var (appointment, sut) = CreateSut(CustomerId, NowUtc.AddHours(1));

        var result = await sut.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("2 saat", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EnumAppointmentStatus.Confirmed, appointment.Status);
    }

    [Fact]
    public async Task Handle_ShouldAllowBusinessOwner_WithinTwoHours()
    {
        var (appointment, sut) = CreateSut(OwnerId, NowUtc.AddHours(1));

        var result = await sut.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EnumAppointmentStatus.Cancelled, appointment.Status);
    }

    [Fact]
    public async Task Handle_ShouldBlockStranger()
    {
        var (appointment, sut) = CreateSut(Guid.NewGuid(), NowUtc.AddHours(3));

        var result = await sut.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(EnumAppointmentStatus.Confirmed, appointment.Status);
    }

    private static CancelAppointmentCommand NewCommand() => new()
    {
        AppointmentId = AppointmentId,
        UserId = CustomerId,
        Reason = "Planim degisti",
        CancelledBy = "test"
    };

    private static (Appointment Appointment, CancelAppointmentCommandHandler Sut) CreateSut(
        Guid callerId,
        DateTime appointmentDate)
    {
        var business = TestData.NewBusiness(OwnerId, BusinessId);
        var employee = TestData.NewEmployee(BusinessId, EmployeeId);

        var appointment = TestData.NewAppointment(
            AppointmentId, CustomerId, BusinessId, ServiceId, EmployeeId,
            appointmentDate, EnumAppointmentStatus.Confirmed);

        NavigationSetter.LinkAppointment(appointment, business, employee);

        var sut = new CancelAppointmentCommandHandler(
            new FakeRepository<Appointment>(new[] { appointment }),
            new StubDateTimeService(NowUtc),
            new FakeUnitOfWork(),
            new AppointManMetrics(),
            TestCurrentUserService.For(callerId));

        return (appointment, sut);
    }
}
