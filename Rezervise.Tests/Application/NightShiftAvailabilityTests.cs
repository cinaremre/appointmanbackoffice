using Rezervise.Application.Features.Appointments.Queries.GetAvailableSlots;
using Rezervise.Application.Features.Employees.Queries.GetAvailableEmployees;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// Gece vardiyası senaryosu (seed test verisi): tam gece açık işletme (00:00-24:00)
/// ve yerel gece yarısından sabah 08:00'e kadar çalışan personel.
/// Kullanıcının raporladığı sorgu: appointmentDate yerel gece yarısı (UTC-3) olduğunda
/// liste boş geliyordu; vardiya gece yarısını kapsayan personelle dolu gelmelidir.
/// </summary>
public class NightShiftAvailabilityTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    // 2026-12-21 Pazartesi yerel günü; yerel saat = UTC + 3.
    // Yerel gece yarısı = 2026-12-20T21:00Z (kullanıcının raporladığı sorgu).
    private static readonly DateTime LocalMidnightUtc = new(2026, 12, 20, 21, 0, 0, DateTimeKind.Utc);
    // Yerel öğlen 12:00 = 2026-12-21T09:00Z (kullanıcının "personel var" dediği sorgu).
    private static readonly DateTime LocalNoonUtc = new(2026, 12, 21, 9, 0, 0, DateTimeKind.Utc);
    // Referans "şimdi": yerel 20 Aralık 23:00 — gece slotları gelecek saatlere düşer.
    private static readonly DateTime NowUtc = new(2026, 12, 20, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AvailableEmployees_AtLocalMidnight_ReturnsNightShiftEmployee()
    {
        var employee = NewNightEmployee();
        var sut = CreateAvailableEmployeesSut(employee, Array.Empty<Appointment>());

        var result = await sut.Handle(NewEmployeesQuery(LocalMidnightUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(EmployeeId, result.Value[0].Id);
    }

    [Fact]
    public async Task AvailableEmployees_AtLocalNoon_ReturnsEmpty_ForNightShiftOnlyEmployee()
    {
        // Nur yalnızca 00:00-08:00 arasında çalışır; öğlen listesinin boş olması
        // vardiya dışı saatte DOĞRU davranıştır (kullanıcının mevcut seed'inde
        // "öğlen personel geliyor, gece boş" gözlemi mesai saatlerinden kaynaklanır).
        var employee = NewNightEmployee();
        var sut = CreateAvailableEmployeesSut(employee, Array.Empty<Appointment>());

        var result = await sut.Handle(NewEmployeesQuery(LocalNoonUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task AvailableEmployees_AtLocalMidnight_ReturnsEmployee_WithoutOwnWorkingHours()
    {
        // Kendi mesai kaydı olmayan personel işletme mesaisini (00:00-24:00) miras alır;
        // hem gece yarısı hem öğlen aynı personel döner ("ikisi de aynı personel" senaryosu).
        var employee = TestData.NewEmployee(BusinessId, EmployeeId);
        TestData.AddEmployeeService(employee, ServiceId);
        var sut = CreateAvailableEmployeesSut(employee, Array.Empty<Appointment>());

        var midnight = await sut.Handle(NewEmployeesQuery(LocalMidnightUtc), CancellationToken.None);
        var noon = await sut.Handle(NewEmployeesQuery(LocalNoonUtc), CancellationToken.None);

        Assert.True(midnight.IsSuccess);
        Assert.True(noon.IsSuccess);
        Assert.Single(midnight.Value);
        Assert.Single(noon.Value);
        Assert.Equal(EmployeeId, midnight.Value[0].Id);
        Assert.Equal(EmployeeId, noon.Value[0].Id);
    }

    [Fact]
    public async Task AvailableSlots_ForNightShiftEmployee_ProducesAvailableSlotsWithinShift()
    {
        var employee = NewNightEmployee();
        var sut = CreateAvailableSlotsSut(employee, Array.Empty<Appointment>());

        var result = await sut.Handle(NewSlotsQuery(LocalMidnightUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var slots = result.Value!;
        Assert.NotEmpty(slots);

        // Tüm slotlar müsait: işletme 00:00-24:00 açık, personel 00:00-08:00 mesaide,
        // çakışma yok, referans zaman yerel 23:00 (gece slotları gelecekte).
        Assert.All(slots, s => Assert.True(s.IsAvailable));

        // Slotlar yalnızca vardiyada üretilmeli: yerel 00:00-08:00 (UTC 21:00-05:00).
        var first = slots[0];
        Assert.Equal(new DateTime(2026, 12, 20, 21, 0, 0, DateTimeKind.Utc), first.StartTime);
        Assert.Equal(new DateTime(2026, 12, 20, 21, 30, 0, DateTimeKind.Utc), first.EndTime);

        var last = slots[^1];
        Assert.Equal(new DateTime(2026, 12, 21, 4, 30, 0, DateTimeKind.Utc), last.StartTime);
        Assert.Equal(new DateTime(2026, 12, 21, 5, 0, 0, DateTimeKind.Utc), last.EndTime);

        // 00:00-08:00 arası 30 dk hizmet, 15 dk adım → 31 slot.
        Assert.Equal(31, slots.Count);
    }

    private static Employee NewNightEmployee()
    {
        var employee = TestData.NewEmployee(BusinessId, EmployeeId);
        TestData.AddEmployeeService(employee, ServiceId);
        TestData.AddEmployeeWorkingHour(employee, DayOfWeek.Monday,
            TimeSpan.FromHours(0), TimeSpan.FromHours(8));
        return employee;
    }

    private static GetAvailableEmployeesQuery NewEmployeesQuery(DateTime appointmentDateUtc) => new()
    {
        BusinessId = BusinessId,
        ServiceId = ServiceId,
        AppointmentDate = appointmentDateUtc
    };

    private static GetAvailableSlotsQuery NewSlotsQuery(DateTime dateUtc) => new()
    {
        BusinessId = BusinessId,
        EmployeeId = EmployeeId,
        ServiceId = ServiceId,
        Date = dateUtc
    };

    private static GetAvailableEmployeesQueryHandler CreateAvailableEmployeesSut(
        Employee employee, IEnumerable<Appointment> appointments)
    {
        var business = NewNightBusiness();

        return new GetAvailableEmployeesQueryHandler(
            new FakeRepository<Employee>(new[] { employee }),
            new FakeRepository<Appointment>(appointments),
            new FakeRepository<Service>(new[] { TestData.NewService(BusinessId, ServiceId, 30) }),
            new FakeRepository<Business>(new[] { business }),
            new StubDateTimeService(NowUtc));
    }

    private static GetAvailableSlotsQueryHandler CreateAvailableSlotsSut(
        Employee employee, IEnumerable<Appointment> appointments)
    {
        var business = NewNightBusiness();

        return new GetAvailableSlotsQueryHandler(
            new FakeRepository<Business>(new[] { business }),
            new FakeRepository<Employee>(new[] { employee }),
            new FakeRepository<Service>(new[] { TestData.NewService(BusinessId, ServiceId, 30) }),
            new FakeRepository<Appointment>(appointments),
            new StubDateTimeService(NowUtc));
    }

    private static Business NewNightBusiness()
    {
        // Gece Kuaförü: her gün 00:00-24:00 mesai (tam gece açık).
        var business = TestData.NewBusiness(OwnerId, BusinessId);
        TestData.AddBusinessWorkingHour(business, DayOfWeek.Monday,
            TimeSpan.FromHours(0), TimeSpan.FromHours(24));
        return business;
    }
}
