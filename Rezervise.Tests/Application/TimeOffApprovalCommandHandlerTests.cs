using Rezervise.Application.Features.Employees.Commands.ApproveTimeOff;
using Rezervise.Application.Features.Employees.Commands.RejectTimeOff;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// Izin onay/red yetkisi: isletme sahibi (veya yonetici) disindaki kimlikler
/// govde RequesterId'sini degistirse bile islem yapamaz.
/// </summary>
public class TimeOffApprovalCommandHandlerTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid TimeOffId = Guid.NewGuid();

    [Fact]
    public async Task Approve_ShouldApprove_WhenCallerIsBusinessOwner()
    {
        var (timeOff, handler) = CreateApproveHandler(TestCurrentUserService.For(OwnerId));

        var result = await handler.Handle(NewApproveCommand(OwnerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(timeOff.IsApproved);
        Assert.Equal(OwnerId.ToString(), timeOff.ApprovedBy);
    }

    [Fact]
    public async Task Approve_ShouldApprove_WhenCallerIsAdministrator()
    {
        var (timeOff, handler) = CreateApproveHandler(
            TestCurrentUserService.For(OtherUserId, "Admin"));

        var result = await handler.Handle(NewApproveCommand(OtherUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(timeOff.IsApproved);
        Assert.Equal(OtherUserId.ToString(), timeOff.ApprovedBy);
    }

    [Fact]
    public async Task Approve_ShouldFail_WhenCallerIsNotBusinessOwner()
    {
        // Govde RequesterId sahip olarak yazilsa bile JWT kimligi esas alinir.
        var (timeOff, handler) = CreateApproveHandler(TestCurrentUserService.For(OtherUserId));

        var result = await handler.Handle(NewApproveCommand(OwnerId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(timeOff.IsApproved);
    }

    [Fact]
    public async Task Approve_ShouldFail_WhenActiveAppointmentOverlapsTimeOff()
    {
        var appointment = TestData.NewAppointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            BusinessId,
            Guid.NewGuid(),
            EmployeeId,
            new DateTime(2026, 12, 2, 10, 0, 0));

        var (timeOff, handler) = CreateApproveHandler(
            TestCurrentUserService.For(OwnerId),
            new[] { appointment });

        var result = await handler.Handle(NewApproveCommand(OwnerId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        Assert.False(timeOff.IsApproved);
    }

    [Fact]
    public async Task Reject_ShouldSoftDelete_WhenCallerIsBusinessOwner()
    {
        var (timeOff, handler) = CreateRejectHandler(TestCurrentUserService.For(OwnerId));

        var result = await handler.Handle(NewRejectCommand(OwnerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(timeOff.IsDeleted);
    }

    [Fact]
    public async Task Reject_ShouldFail_WhenCallerIsNotBusinessOwner()
    {
        var (timeOff, handler) = CreateRejectHandler(TestCurrentUserService.For(OtherUserId));

        var result = await handler.Handle(NewRejectCommand(OwnerId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(timeOff.IsDeleted);
    }

    [Fact]
    public async Task Reject_ShouldFail_WhenTimeOffAlreadyApproved()
    {
        var (timeOff, handler) = CreateRejectHandler(TestCurrentUserService.For(OwnerId));
        timeOff.Approve(OwnerId.ToString());

        var result = await handler.Handle(NewRejectCommand(OwnerId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
        Assert.False(timeOff.IsDeleted);
    }

    private static ApproveTimeOffCommand NewApproveCommand(Guid requesterId) => new()
    {
        TimeOffId = TimeOffId,
        RequesterId = requesterId
    };

    private static RejectTimeOffCommand NewRejectCommand(Guid requesterId) => new()
    {
        TimeOffId = TimeOffId,
        RequesterId = requesterId
    };

    private static EmployeeTimeOff NewTimeOff()
    {
        var timeOff = new EmployeeTimeOff(
            TimeOffId,
            EmployeeId,
            new DateTime(2026, 12, 1),
            new DateTime(2026, 12, 5),
            EnumTimeOffType.Vacation);

        timeOff.ClearDomainEvents();
        return timeOff;
    }

    private static (EmployeeTimeOff TimeOff, ApproveTimeOffCommandHandler Handler) CreateApproveHandler(
        TestCurrentUserService currentUser,
        IEnumerable<Appointment>? appointments = null)
    {
        var timeOff = NewTimeOff();

        var handler = new ApproveTimeOffCommandHandler(
            new FakeRepository<EmployeeTimeOff>(new[] { timeOff }),
            new FakeRepository<Employee>(new[] { TestData.NewEmployee(BusinessId, EmployeeId) }),
            new FakeRepository<Appointment>(appointments),
            new FakeRepository<Business>(new[] { TestData.NewBusiness(OwnerId, BusinessId) }),
            new FakeUnitOfWork(),
            currentUser);

        return (timeOff, handler);
    }

    private static (EmployeeTimeOff TimeOff, RejectTimeOffCommandHandler Handler) CreateRejectHandler(
        TestCurrentUserService currentUser)
    {
        var timeOff = NewTimeOff();

        var handler = new RejectTimeOffCommandHandler(
            new FakeRepository<EmployeeTimeOff>(new[] { timeOff }),
            new FakeRepository<Employee>(new[] { TestData.NewEmployee(BusinessId, EmployeeId) }),
            new FakeRepository<Business>(new[] { TestData.NewBusiness(OwnerId, BusinessId) }),
            new FakeUnitOfWork(),
            currentUser);

        return (timeOff, handler);
    }
}
