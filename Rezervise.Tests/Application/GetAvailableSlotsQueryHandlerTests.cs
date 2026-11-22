using Rezervise.Application.Features.Appointments.Queries.GetAvailableSlots;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

public class GetAvailableSlotsQueryHandlerTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    // 2026-11-23 Pazartesi, isletme saati +03:00. Yerel gun baslangici UTC 2026-11-22 21:00.
    private static readonly DateTime TargetDateLocal = new(2026, 11, 23, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ReferenceUtcNow = new(2026, 11, 22, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ShouldReturnAvailableSlots_ForWorkingDay()
    {
        var clock = new StubDateTimeService(ReferenceUtcNow);
        var sut = CreateSut(clock);

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var slots = result.Value!;
        Assert.NotEmpty(slots);

        var first = slots[0];
        Assert.Equal(new DateTime(2026, 11, 23, 6, 0, 0, DateTimeKind.Utc), first.StartTime);
        Assert.Equal(new DateTime(2026, 11, 23, 6, 30, 0, DateTimeKind.Utc), first.EndTime);
        Assert.True(slots.All(s => s.IsAvailable));
    }

    [Fact]
    public async Task Handle_ShouldMarkOverlappingSlotUnavailable()
    {
        var clock = new StubDateTimeService(ReferenceUtcNow);
        var conflict = TestData.NewAppointment(
            Guid.NewGuid(), CustomerId, BusinessId, ServiceId, EmployeeId,
            new DateTime(2026, 11, 23, 7, 0, 0, DateTimeKind.Utc),
            endTime: new DateTime(2026, 11, 23, 7, 30, 0, DateTimeKind.Utc));
        var sut = CreateSut(clock, new[] { conflict });

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var slot = result.Value!.Single(s => s.StartTime.Hour == 7 && s.StartTime.Minute == 0);
        Assert.False(slot.IsAvailable);
        Assert.True(result.Value.Single(s => s.StartTime.Hour == 7 && s.StartTime.Minute == 30).IsAvailable);
    }

    [Fact]
    public async Task Handle_ShouldCatchAppointmentSpillingFromPreviousDay()
    {
        var clock = new StubDateTimeService(ReferenceUtcNow);
        // Onceki gun 23:00 lokal baslayan 10,5 saatlik randevu 09:30'da biter.
        var spillover = TestData.NewAppointment(
            Guid.NewGuid(), CustomerId, BusinessId, ServiceId, EmployeeId,
            new DateTime(2026, 11, 22, 20, 0, 0, DateTimeKind.Utc),
            endTime: new DateTime(2026, 11, 23, 6, 30, 0, DateTimeKind.Utc));
        var sut = CreateSut(clock, new[] { spillover });

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Single(s => s.StartTime.Hour == 6 && s.StartTime.Minute == 0).IsAvailable);
        Assert.True(result.Value.Single(s => s.StartTime.Hour == 6 && s.StartTime.Minute == 30).IsAvailable);
    }

    [Fact]
    public async Task Handle_ShouldMarkBreakSlotUnavailable()
    {
        var clock = new StubDateTimeService(ReferenceUtcNow);
        var sut = CreateSut(clock, breakStart: TimeSpan.FromHours(12), breakEnd: TimeSpan.FromHours(13));

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Single(s => s.StartTime.Hour == 9 && s.StartTime.Minute == 0).IsAvailable);
        Assert.True(result.Value.Single(s => s.StartTime.Hour == 10 && s.StartTime.Minute == 0).IsAvailable);
    }

    [Fact]
    public async Task Handle_ShouldMarkApprovedTimeOffUnavailable()
    {
        var clock = new StubDateTimeService(ReferenceUtcNow);
        var employee = CreateEmployee(clock);
        var timeOff = new EmployeeTimeOff(
            employee.Id,
            new DateTime(2026, 11, 23, 11, 0, 0),
            new DateTime(2026, 11, 23, 13, 0, 0),
            EnumTimeOffType.Vacation);
        timeOff.Approve(OwnerId.ToString());
        employee.AddTimeOff(timeOff);

        var sut = new GetAvailableSlotsQueryHandler(
            new FakeRepository<Business>(new[] { CreateBusiness() }),
            new FakeRepository<Employee>(new[] { employee }),
            new FakeRepository<Service>(new[] { CreateService() }),
            new FakeRepository<Appointment>(),
            clock);

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Single(s => s.StartTime.Hour == 8 && s.StartTime.Minute == 0).IsAvailable);
    }

    [Fact]
    public async Task Handle_ShouldNotLeakExceptionMessage()
    {
        var clock = new StubDateTimeService(ReferenceUtcNow);
        var throwing = new ThrowingRepository<Appointment>();
        var sut = new GetAvailableSlotsQueryHandler(
            new FakeRepository<Business>(new[] { CreateBusiness() }),
            new FakeRepository<Employee>(new[] { CreateEmployee(clock) }),
            new FakeRepository<Service>(new[] { CreateService() }),
            throwing,
            clock);

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.DoesNotContain("secret", result.Error!.Detail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static GetAvailableSlotsQuery NewQuery() => new()
    {
        BusinessId = BusinessId,
        EmployeeId = EmployeeId,
        ServiceId = ServiceId,
        Date = TargetDateLocal
    };

    private static Business CreateBusiness()
    {
        var business = TestData.NewBusiness(OwnerId, BusinessId);
        TestData.AddBusinessWorkingHour(business, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18));
        return business;
    }

    private static Employee CreateEmployee(
        StubDateTimeService clock,
        TimeSpan? breakStart = null,
        TimeSpan? breakEnd = null)
    {
        var employee = TestData.NewEmployee(BusinessId, EmployeeId);
        TestData.AddEmployeeService(employee, ServiceId);
        TestData.AddEmployeeWorkingHour(
            employee, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18),
            breakStart, breakEnd);
        return employee;
    }

    private static Service CreateService() => TestData.NewService(BusinessId, ServiceId, 30);

    private static GetAvailableSlotsQueryHandler CreateSut(
        StubDateTimeService clock,
        IReadOnlyList<Appointment>? appointments = null,
        TimeSpan? breakStart = null,
        TimeSpan? breakEnd = null)
        => new(
            new FakeRepository<Business>(new[] { CreateBusiness() }),
            new FakeRepository<Employee>(new[] { CreateEmployee(clock, breakStart, breakEnd) }),
            new FakeRepository<Service>(new[] { CreateService() }),
            new FakeRepository<Appointment>(appointments),
            clock);
}
