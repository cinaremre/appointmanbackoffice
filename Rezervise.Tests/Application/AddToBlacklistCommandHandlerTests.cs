using Rezervise.Application.Features.Blacklists.Commands.AddToBlacklist;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// Kara listeye ekleme: yetki (isletme sahibi veya calisan) ve ayni isletmedeki
/// acik randevularin otomatik iptali.
/// </summary>
public class AddToBlacklistCommandHandlerTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OtherBusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid EmployeeUserId = Guid.NewGuid();

    private static readonly DateTime NowUtc = new(2026, 11, 22, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ShouldCancelOpenAppointments_OfThatBusiness()
    {
        var openOne = NewAppointment(NowUtc.AddDays(1), EnumAppointmentStatus.Pending);
        var openTwo = NewAppointment(NowUtc.AddDays(2), EnumAppointmentStatus.Confirmed);
        var otherBusinessOpen = NewAppointment(NowUtc.AddDays(1), EnumAppointmentStatus.Confirmed, OtherBusinessId);
        var past = NewAppointment(NowUtc.AddDays(-3), EnumAppointmentStatus.Confirmed);
        var alreadyCancelled = NewAppointment(NowUtc.AddDays(1), EnumAppointmentStatus.Cancelled);

        var (sut, blacklist) = CreateSut(
            OwnerId,
            new[] { openOne, openTwo, otherBusinessOpen, past, alreadyCancelled });

        var result = await sut.Handle(NewCommand(OwnerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EnumAppointmentStatus.Cancelled, openOne.Status);
        Assert.Equal(EnumAppointmentStatus.Cancelled, openTwo.Status);

        // Baska isletmedeki, gecmis ve zaten iptal edilmis randevulara dokunulmaz.
        Assert.Equal(EnumAppointmentStatus.Confirmed, otherBusinessOpen.Status);
        Assert.Equal(EnumAppointmentStatus.Confirmed, past.Status);
        Assert.Equal(EnumAppointmentStatus.Cancelled, alreadyCancelled.Status);

        var entry = Assert.Single(blacklist.Items);
        Assert.Equal(2, entry.AppointmentCount);
    }

    [Fact]
    public async Task Handle_ShouldAllowEmployee_OfThatBusiness()
    {
        var open = NewAppointment(NowUtc.AddDays(1), EnumAppointmentStatus.Confirmed);
        var (sut, blacklist) = CreateSut(EmployeeUserId, new[] { open }, linkEmployeeToUser: true);

        var result = await sut.Handle(NewCommand(EmployeeUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(blacklist.Items);
        Assert.Equal(EnumAppointmentStatus.Cancelled, open.Status);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCallerIsNeitherOwnerNorEmployee()
    {
        var open = NewAppointment(NowUtc.AddDays(1), EnumAppointmentStatus.Confirmed);
        var (sut, blacklist) = CreateSut(Guid.NewGuid(), new[] { open });

        var result = await sut.Handle(NewCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(blacklist.Items);
        Assert.Equal(EnumAppointmentStatus.Confirmed, open.Status);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenCustomerDoesNotExist()
    {
        var (sut, _) = CreateSut(OwnerId, Array.Empty<Appointment>(), includeCustomer: false);

        var result = await sut.Handle(NewCommand(OwnerId), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private static AddToBlacklistCommand NewCommand(Guid requesterId) => new()
    {
        BusinessId = BusinessId,
        UserId = CustomerId,
        Reason = "Randevuya gelmedi",
        RequesterId = requesterId
    };

    private static Appointment NewAppointment(
        DateTime appointmentDate,
        EnumAppointmentStatus status,
        Guid? businessId = null)
        => TestData.NewAppointment(
            Guid.NewGuid(), CustomerId, businessId ?? BusinessId, ServiceId, EmployeeId,
            appointmentDate, status);

    private static (AddToBlacklistCommandHandler Sut, FakeRepository<BlacklistEntry> Blacklist) CreateSut(
        Guid callerId,
        IEnumerable<Appointment> appointments,
        bool linkEmployeeToUser = false,
        bool includeCustomer = true)
    {
        var business = TestData.NewBusiness(OwnerId, BusinessId);
        var employee = TestData.NewEmployee(BusinessId, EmployeeId);

        if (linkEmployeeToUser)
            employee.LinkToUser(TestData.NewUser(EmployeeUserId));

        var blacklist = new FakeRepository<BlacklistEntry>();
        var users = includeCustomer
            ? new FakeRepository<User>(new[] { TestData.NewUser(CustomerId) })
            : new FakeRepository<User>();

        var sut = new AddToBlacklistCommandHandler(
            blacklist,
            new FakeRepository<Business>(new[] { business }),
            users,
            new FakeRepository<Appointment>(appointments),
            new FakeRepository<Employee>(new[] { employee }),
            new FakeUnitOfWork(),
            new StubDateTimeService(NowUtc));

        // callerId yalnizca yetki senaryosunu belirlemek icin kullanilir.
        _ = callerId;

        return (sut, blacklist);
    }
}
