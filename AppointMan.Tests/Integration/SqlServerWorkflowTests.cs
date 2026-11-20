using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Events;
using AppointMan.Domain.Interfaces;
using AppointMan.Domain.ValueObjects;
using AppointMan.Infrastructure.Persistence.Context;
using AppointMan.Infrastructure.Persistence.Services;
using Microsoft.EntityFrameworkCore;

namespace AppointMan.Tests.Integration;

[Collection("SqlServerIntegration")]
public sealed class SqlServerWorkflowTests(SqlServerDatabaseFixture fixture)
{
    private static int _phoneCounter;
    [Fact]
    public async Task RatingRecalculate_UpdatesCountAndAverageFromVisibleReviews()
    {
        await using var context = fixture.CreateContext();

        var owner = NewPersistableUser();
        var customer = NewPersistableUser();
        var business = TestData.NewBusiness(owner.Id);
        var service = NewPersistableService(business.Id, out var category);
        var employee = TestData.NewEmployee(business.Id);
        var date = DateTime.UtcNow.AddDays(2);

        var appointment1 = TestData.NewAppointment(Guid.NewGuid(), customer.Id, business.Id, service.Id, employee.Id, date, EnumAppointmentStatus.Completed);
        var appointment2 = TestData.NewAppointment(Guid.NewGuid(), customer.Id, business.Id, service.Id, employee.Id, date.AddMinutes(45), EnumAppointmentStatus.Completed);
        var review1 = TestData.NewReview(Guid.NewGuid(), customer.Id, business.Id, appointment1.Id, 5);
        var review2 = TestData.NewReview(Guid.NewGuid(), customer.Id, business.Id, appointment2.Id, 3);

        context.AddRange(owner, customer, business, category, service, employee, appointment1, appointment2, review1, review2);
        await context.SaveChangesAsync();

        var updater = new BusinessRatingUpdater(context);
        await updater.UpdateOnNewReviewAsync(business.Id, 5);
        await updater.UpdateOnNewReviewAsync(business.Id, 3);

        await context.Entry(business).ReloadAsync();
        Assert.Equal(2, business.TotalReviews);
        Assert.Equal(4.0m, business.Rating);

        review1.Hide();
        await context.SaveChangesAsync();
        await updater.RecalculateAsync(business.Id);

        await context.Entry(business).ReloadAsync();
        Assert.Equal(1, business.TotalReviews);
        Assert.Equal(3.0m, business.Rating);

        review1.Show();
        review2.Delete();
        await context.SaveChangesAsync();
        await updater.RecalculateAsync(business.Id);

        await context.Entry(business).ReloadAsync();
        Assert.Equal(1, business.TotalReviews);
        Assert.Equal(5.0m, business.Rating);
    }

    [Fact]
    public async Task ActiveAppointmentUniqueIndex_BlocksDuplicateAndAllowsCancelledSlot()
    {
        await using var context = fixture.CreateContext();

        var owner = NewPersistableUser();
        var customer = NewPersistableUser();
        var business = TestData.NewBusiness(owner.Id);
        var service = NewPersistableService(business.Id, out var category);
        var employee = TestData.NewEmployee(business.Id);
        var date = DateTime.UtcNow.AddDays(3);

        context.AddRange(owner, customer, business, category, service, employee);
        await context.SaveChangesAsync();

        var first = TestData.NewAppointment(Guid.NewGuid(), customer.Id, business.Id, service.Id, employee.Id, date);
        context.Add(first);
        await context.SaveChangesAsync();

        var duplicate = TestData.NewAppointment(Guid.NewGuid(), customer.Id, business.Id, service.Id, employee.Id, date);
        context.Add(duplicate);
        await Assert.ThrowsAsync<DuplicateKeyException>(() => context.SaveChangesAsync());

        context.Entry(duplicate).State = EntityState.Detached;
        first.SetStatusForSeed(EnumAppointmentStatus.Cancelled, cancelledAt: DateTime.UtcNow);
        await context.SaveChangesAsync();

        var replacement = TestData.NewAppointment(Guid.NewGuid(), customer.Id, business.Id, service.Id, employee.Id, date);
        context.Add(replacement);
        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Appointments.CountAsync(a => a.EmployeeId == employee.Id && a.AppointmentDate == date));
    }

    [Fact]
    public async Task SaveChanges_DispatchesDomainEventsAfterPersistence()
    {
        var dispatcher = new RecordingDomainEventDispatcher();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(fixture.ConnectionString)
            .Options;

        await using var context = new AppDbContext(options, dispatcher);

        var user = new User(
            Guid.NewGuid(),
            "Event",
            "Test",
            new Email($"event-{Guid.NewGuid():N}@test.com"),
            new PhoneNumber("+905550000099"),
            EnumUserRole.Customer);

        user.SetPasswordHash("test-password-hash");
        context.Add(user);
        await context.SaveChangesAsync();

        Assert.NotEmpty(dispatcher.Dispatched);
        Assert.Contains(dispatcher.Dispatched, e => e.GetType().Name == "UserRegisteredEvent");
    }

    private static Service NewPersistableService(Guid businessId, out ServiceCategory category)
    {
        category = new ServiceCategory(displayOrder: 0);
        category.ClearDomainEvents();

        var service = new Service(
            Guid.NewGuid(),
            "Test Hizmeti",
            new Money(100m),
            30,
            category.Id,
            businessId);

        service.ClearDomainEvents();
        return service;
    }

    private static User NewPersistableUser()
    {
        var id = Guid.NewGuid();
        var phone = $"+90555{Interlocked.Increment(ref _phoneCounter):D7}";
        var user = new User(
            id,
            "Test",
            "Kullanici",
            new Email($"user-{id:N}@test.com"),
            new PhoneNumber(phone),
            EnumUserRole.Customer);

        user.SetPasswordHash("test-password-hash");
        user.ClearDomainEvents();
        return user;
    }

    private sealed class RecordingDomainEventDispatcher : IDomainEventDispatcher
    {
        public List<IDomainEvent> Dispatched { get; } = [];

        public Task DispatchAsync(
            IEnumerable<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
        {
            Dispatched.AddRange(domainEvents);
            return Task.CompletedTask;
        }
    }
}
