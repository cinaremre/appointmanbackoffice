using AppointMan.Application.Features.Reviews.Commands.AddReview;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

public class AddReviewCommandHandlerTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OtherBusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ShouldReject_WhenCallerIsAnotherUser()
    {
        var appointment = CreateAppointment(EnumAppointmentStatus.Completed);
        var sut = CreateSut(
            TestCurrentUserService.For(Guid.NewGuid()),
            appointment);

        var result = await sut.Handle(NewCommand(appointment.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenAppointmentMissing()
    {
        var sut = CreateSut(TestCurrentUserService.For(CustomerId));

        var result = await sut.Handle(NewCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Theory]
    [InlineData(EnumAppointmentStatus.Pending)]
    [InlineData(EnumAppointmentStatus.Confirmed)]
    [InlineData(EnumAppointmentStatus.Cancelled)]
    public async Task Handle_ShouldReject_WhenAppointmentIsNotCompleted(EnumAppointmentStatus status)
    {
        var appointment = CreateAppointment(status);
        var sut = CreateSut(TestCurrentUserService.For(CustomerId), appointment);

        var result = await sut.Handle(NewCommand(appointment.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenAppointmentBelongsToAnotherBusiness()
    {
        var appointment = CreateAppointment(EnumAppointmentStatus.Completed);
        var sut = CreateSut(TestCurrentUserService.For(CustomerId), appointment);
        var command = NewCommand(appointment.Id, OtherBusinessId);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ShouldCreateReview_ForCompletedOwnAppointment()
    {
        var appointment = CreateAppointment(EnumAppointmentStatus.Completed);
        var reviews = new FakeRepository<Review>();
        var sut = CreateSut(TestCurrentUserService.For(CustomerId), appointment, reviews);

        var result = await sut.Handle(NewCommand(appointment.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(reviews.Items);
        Assert.Equal(CustomerId, reviews.Items[0].UserId);
        Assert.Equal(appointment.Id, reviews.Items[0].AppointmentId);
    }

    private static AddReviewCommand NewCommand(Guid appointmentId, Guid? businessId = null) => new()
    {
        UserId = CustomerId,
        BusinessId = businessId ?? BusinessId,
        AppointmentId = appointmentId,
        Rating = 5,
        Comment = "Randevu gercekten cok guzeldi."
    };

    private static Appointment CreateAppointment(EnumAppointmentStatus status)
        => TestData.NewAppointment(
            Guid.NewGuid(), CustomerId, BusinessId, ServiceId, EmployeeId,
            DateTime.UtcNow.AddDays(2), status);

    private static AddReviewCommandHandler CreateSut(
        TestCurrentUserService currentUser,
        Appointment? appointment = null,
        FakeRepository<Review>? reviews = null)
    {
        reviews ??= new FakeRepository<Review>();

        return new AddReviewCommandHandler(
            reviews,
            new FakeRepository<AppointMan.Domain.Entities.User>(new[] { TestData.NewUser(CustomerId) }),
            new FakeRepository<AppointMan.Domain.Entities.Business>(new[]
            {
                TestData.NewBusiness(OwnerId, BusinessId),
                TestData.NewBusiness(OwnerId, OtherBusinessId)
            }),
            new FakeRepository<Appointment>(appointment is null ? null : new[] { appointment }),
            new StubBusinessRatingUpdater(),
            new FakeUnitOfWork(),
            currentUser);
    }
}
