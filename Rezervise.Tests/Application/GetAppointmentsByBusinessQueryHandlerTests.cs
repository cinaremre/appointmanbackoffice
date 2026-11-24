using Rezervise.Application.Features.Appointments.Queries.GetAppointmentsByBusiness;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// İşletmenin randevu listesi: userFullName ve serviceName (ayrıca employeeFullName)
/// navigasyonlar yüklendiği için dolu gelmeli; liste detay endpoint'i kadar dolu olmalı.
/// </summary>
public class GetAppointmentsByBusinessQueryHandlerTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ShouldFillUserFullNameAndServiceName()
    {
        var user = NewUser("Ahmet", "Çelik");
        var service = TestData.NewService(BusinessId, ServiceId, 30);
        service.SetName("Saç Kesimi");
        var employee = TestData.NewEmployee(BusinessId, EmployeeId);

        var appointment = TestData.NewAppointment(
            Guid.NewGuid(), CustomerId, BusinessId, ServiceId, EmployeeId,
            new DateTime(2026, 12, 21, 6, 0, 0, DateTimeKind.Utc));
        NavigationSetter.LinkAppointment(appointment, NewBusiness(), employee);
        NavigationSetter.SetService(appointment, service);
        NavigationSetter.SetUser(appointment, user);

        var sut = CreateSut(new[] { appointment });

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value);
        Assert.Equal("Ahmet Çelik", dto.UserFullName);
        Assert.Equal("Saç Kesimi", dto.ServiceName);
        Assert.NotNull(dto.EmployeeFullName);
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenCallerIsNotBusinessOwner()
    {
        var appointment = TestData.NewAppointment(
            Guid.NewGuid(), CustomerId, BusinessId, ServiceId, EmployeeId,
            new DateTime(2026, 12, 21, 6, 0, 0, DateTimeKind.Utc));

        // Başka bir kullanıcı (owner değil) sorgu yaptığında liste görünmemeli.
        var sut = CreateSut(new[] { appointment }, userId: Guid.NewGuid());

        var result = await sut.Handle(NewQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    private static GetAppointmentsByBusinessQuery NewQuery() => new()
    {
        BusinessId = BusinessId,
        PageNumber = 1,
        PageSize = 10
    };

    private static GetAppointmentsByBusinessQueryHandler CreateSut(
        IEnumerable<Appointment> appointments,
        Guid? userId = null)
    {
        var business = NewBusiness();
        var caller = TestCurrentUserService.For(userId ?? OwnerId);

        return new GetAppointmentsByBusinessQueryHandler(
            new FakeRepository<Appointment>(appointments),
            new FakeRepository<Business>(new[] { business }),
            caller);
    }

    private static Business NewBusiness()
        => TestData.NewBusiness(OwnerId, BusinessId);

    private static User NewUser(string firstName, string lastName)
    {
        var user = new User(
            CustomerId, firstName, lastName,
            new Rezervise.Domain.ValueObjects.Email($"user-{Guid.NewGuid():N}@test.com"),
            new Rezervise.Domain.ValueObjects.PhoneNumber($"+90555{Guid.NewGuid().ToString("N")[..8]}"),
            EnumUserRole.Customer);
        return user;
    }
}
