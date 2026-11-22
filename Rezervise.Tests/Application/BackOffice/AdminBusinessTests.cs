using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Features.Admin.Businesses.Commands.ActivateBusinessByAdmin;
using Rezervise.Application.Features.Admin.Businesses.Commands.SuspendBusinessByAdmin;
using Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinessAuditTrail;
using Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinessDetail;
using Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinesses;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application.BackOffice;

/// <summary>
/// Isletme yonetimi handler testleri (dokuman Modul 8). Yetki kontrolleri
/// controller/policy katmanindadir; burada is kurallari dogrulanir.
/// </summary>
public class AdminBusinessTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    /// <summary>
    /// Denetim kayitlarini FakeRepository'ye yazan IAdminAuditSink fake'i.
    /// Gercek sink'in kalici yazma davranisina kucuk olcekli karsilik.
    /// </summary>
    private sealed class RecordingAuditSink : IAdminAuditSink
    {
        private readonly FakeRepository<AdminAuditLog> _repository;

        public RecordingAuditSink(FakeRepository<AdminAuditLog> repository)
        {
            _repository = repository;
        }

        public async Task RecordAsync(
            Guid actorUserId,
            string actorRole,
            Guid? businessId,
            string entityType,
            string entityId,
            string action,
            string? oldValues = null,
            string? newValues = null,
            string? reason = null,
            string? correlationId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            var log = new AdminAuditLog(
                actorUserId,
                actorRole,
                businessId,
                entityType,
                entityId,
                action,
                DateTime.UtcNow,
                oldValues,
                newValues,
                reason,
                correlationId,
                ipAddress);

            await _repository.AddAsync(log, cancellationToken);
        }
    }

    /// <summary>Sabit denetim baglami: test correlation ve loopback adres.</summary>
    private sealed class TestAuditContext : IAdminAuditContext
    {
        public string CorrelationId => "test";

        public string IpAddress => "127.0.0.1";
    }

    // ===== GetBusinesses =====

    [Fact]
    public async Task GetBusinesses_ShouldFilterByStatus()
    {
        var active = TestData.NewBusiness(Guid.NewGuid());
        var suspended = TestData.NewBusiness(Guid.NewGuid());
        suspended.Suspend("tester", "test durumu");

        var handler = CreateGetBusinessesHandler(active, suspended);

        var result = await handler.Handle(
            new GetBusinessesQuery { Status = "Suspended" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(suspended.Id, item.Id);
        Assert.Equal("Suspended", item.Status);
        Assert.Equal(1L, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetBusinesses_ShouldFilterBySearch()
    {
        var alpha = TestData.NewBusiness(Guid.NewGuid());
        alpha.SetName("Alpha Studio");
        var beta = TestData.NewBusiness(Guid.NewGuid());
        beta.SetName("Beta Studio");

        var handler = CreateGetBusinessesHandler(alpha, beta);

        var result = await handler.Handle(
            new GetBusinessesQuery { Search = "alpha" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(alpha.Id, item.Id);
    }

    [Fact]
    public async Task GetBusinesses_ShouldClampPageSizeOver200()
    {
        var first = TestData.NewBusiness(Guid.NewGuid());
        var second = TestData.NewBusiness(Guid.NewGuid());

        var handler = CreateGetBusinessesHandler(first, second);

        var result = await handler.Handle(
            new GetBusinessesQuery { Page = 1, PageSize = 500 }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // 200 uzeri sayfa boyutu 200'e cekilir; toplam sayi degismez.
        Assert.Equal(200, result.Value!.PageSize);
        Assert.Equal(2L, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetBusinesses_ShouldDefaultPageSizeWhenNotPositive()
    {
        var handler = CreateGetBusinessesHandler(TestData.NewBusiness(Guid.NewGuid()));

        var result = await handler.Handle(
            new GetBusinessesQuery { PageSize = 0 }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value!.PageSize);
    }

    // ===== SuspendBusinessByAdmin =====

    [Fact]
    public async Task Suspend_ShouldFail_WhenReasonEmpty()
    {
        var business = TestData.NewBusiness(Guid.NewGuid());
        var auditLogs = new FakeRepository<AdminAuditLog>();

        var handler = CreateSuspendHandler(business, auditLogs);

        var result = await handler.Handle(
            new SuspendBusinessByAdminCommand { BusinessId = business.Id, Reason = "   " },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        Assert.True(business.IsActive);
        Assert.Empty(auditLogs.Items);
    }

    [Fact]
    public async Task Suspend_ShouldSuspendBusinessAndWriteAudit_WhenReasonGiven()
    {
        var business = TestData.NewBusiness(Guid.NewGuid());
        var auditLogs = new FakeRepository<AdminAuditLog>();

        var handler = CreateSuspendHandler(business, auditLogs);

        var result = await handler.Handle(
            new SuspendBusinessByAdminCommand { BusinessId = business.Id, Reason = "Cok sayida sikayet" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(business.IsActive);

        var log = Assert.Single(auditLogs.Items);
        Assert.Equal("Suspend", log.Action);
        Assert.Equal("Business", log.EntityType);
        Assert.Equal(business.Id, log.BusinessId);
        Assert.Equal(AdminId, log.ActorUserId);
        Assert.Equal("PlatformAdmin", log.ActorRole);
        Assert.Equal("Cok sayida sikayet", log.Reason);
        Assert.Contains("Active", log.OldValues);
        Assert.Contains("Suspended", log.NewValues);
        Assert.Equal("test", log.CorrelationId);
        Assert.Equal("127.0.0.1", log.IpAddress);
    }

    [Fact]
    public async Task Suspend_ShouldFail_WhenBusinessAlreadySuspended()
    {
        var business = TestData.NewBusiness(Guid.NewGuid());
        business.Suspend("tester", "onceki durum");

        var handler = CreateSuspendHandler(business, new FakeRepository<AdminAuditLog>());

        var result = await handler.Handle(
            new SuspendBusinessByAdminCommand { BusinessId = business.Id, Reason = "tekrar" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
    }

    [Fact]
    public async Task Suspend_ShouldFail_WhenBusinessNotFound()
    {
        var handler = CreateSuspendHandler(
            TestData.NewBusiness(Guid.NewGuid()), new FakeRepository<AdminAuditLog>());

        var result = await handler.Handle(
            new SuspendBusinessByAdminCommand { BusinessId = Guid.NewGuid(), Reason = "yok" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    // ===== ActivateBusinessByAdmin =====

    [Fact]
    public async Task Activate_ShouldActivateBusinessAndWriteAudit()
    {
        var business = TestData.NewBusiness(Guid.NewGuid());
        business.Suspend("tester", "test");

        var auditLogs = new FakeRepository<AdminAuditLog>();
        var handler = CreateActivateHandler(business, auditLogs);

        var result = await handler.Handle(
            new ActivateBusinessByAdminCommand { BusinessId = business.Id, Reason = "Sikayet cozuldu" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(business.IsActive);

        var log = Assert.Single(auditLogs.Items);
        Assert.Equal("Activate", log.Action);
        Assert.Equal(AdminId, log.ActorUserId);
        // Old/New degerler dokumandaki gibi kisa JSON betimlemedir.
        Assert.Equal("""{"Status":"Suspended"}""", log.OldValues);
        Assert.Equal("""{"Status":"Active"}""", log.NewValues);
    }

    [Fact]
    public async Task Activate_ShouldFail_WhenBusinessAlreadyActive()
    {
        var business = TestData.NewBusiness(Guid.NewGuid());

        var handler = CreateActivateHandler(business, new FakeRepository<AdminAuditLog>());

        var result = await handler.Handle(
            new ActivateBusinessByAdminCommand { BusinessId = business.Id }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
    }

    // ===== GetBusinessDetail =====

    [Fact]
    public async Task GetBusinessDetail_ShouldFail_WhenBusinessNotFound()
    {
        var handler = CreateGetBusinessDetailHandler(
            TestData.NewBusiness(Guid.NewGuid()),
            Array.Empty<Employee>(),
            Array.Empty<Service>(),
            Array.Empty<Appointment>());

        var result = await handler.Handle(
            new GetBusinessDetailQuery { BusinessId = Guid.NewGuid() }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task GetBusinessDetail_ShouldReturnCounts()
    {
        var business = TestData.NewBusiness(Guid.NewGuid());

        var employee = TestData.NewEmployee(business.Id);
        var otherEmployee = TestData.NewEmployee(Guid.NewGuid());

        var service = TestData.NewService(business.Id, Guid.NewGuid());
        var inactiveService = TestData.NewService(business.Id, Guid.NewGuid());
        inactiveService.Deactivate();

        var recentAppointment = TestData.NewAppointment(
            Guid.NewGuid(), Guid.NewGuid(), business.Id, Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-1), EnumAppointmentStatus.Completed);

        var oldAppointment = TestData.NewAppointment(
            Guid.NewGuid(), Guid.NewGuid(), business.Id, Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-45), EnumAppointmentStatus.Completed);

        var handler = CreateGetBusinessDetailHandler(
            business,
            new[] { employee, otherEmployee },
            new[] { service, inactiveService },
            new[] { recentAppointment, oldAppointment });

        var result = await handler.Handle(
            new GetBusinessDetailQuery { BusinessId = business.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.Equal(business.Name, dto.Name);
        Assert.Equal("Active", dto.Status);
        Assert.Equal(1, dto.EmployeeCount);
        Assert.Equal(1, dto.ActiveServiceCount);
        // Son 30 gunun randevulari: sadece recentAppointment sayilir.
        Assert.Equal(1, dto.RecentAppointmentCount);
    }

    // ===== GetBusinessAuditTrail =====

    [Fact]
    public async Task GetAuditTrail_ShouldReturnLogsNewestFirst()
    {
        var business = TestData.NewBusiness(Guid.NewGuid());

        // Kayit zamanlari test icin sabitlenir; siralama deterministik olur.
        var older = new AdminAuditLog(
            AdminId, "PlatformAdmin", business.Id, "Business", business.Id.ToString(),
            "Suspend", DateTime.UtcNow.AddMinutes(-5), reason: "Ilgili");

        var newer = new AdminAuditLog(
            AdminId, "PlatformAdmin", business.Id, "Business", business.Id.ToString(),
            "Activate", DateTime.UtcNow);

        var auditLogs = new FakeRepository<AdminAuditLog>(new[] { older, newer });
        var trailHandler = new GetBusinessAuditTrailQueryHandler(auditLogs);

        var result = await trailHandler.Handle(
            new GetBusinessAuditTrailQuery { BusinessId = business.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2L, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);

        // En yeni kayit (Activate) listede once gelir.
        Assert.Equal("Activate", result.Value.Items[0].Action);
        Assert.Equal("Suspend", result.Value.Items[1].Action);
        Assert.Equal(AdminId, result.Value.Items[0].ActorUserId);
        Assert.Equal("PlatformAdmin", result.Value.Items[0].ActorRole);
        Assert.Equal("Ilgili", result.Value.Items[1].Reason);
    }

    // ===== Handler fabrikalari =====

    private static GetBusinessesQueryHandler CreateGetBusinessesHandler(params Business[] businesses)
        => new(new FakeRepository<Business>(businesses));

    private static SuspendBusinessByAdminCommandHandler CreateSuspendHandler(
        Business business,
        FakeRepository<AdminAuditLog> auditLogs)
        => new(
            new FakeRepository<Business>(new[] { business }),
            new FakeUnitOfWork(),
            TestCurrentUserService.For(AdminId, "PlatformAdmin"),
            new RecordingAuditSink(auditLogs),
            new TestAuditContext());

    private static ActivateBusinessByAdminCommandHandler CreateActivateHandler(
        Business business,
        FakeRepository<AdminAuditLog> auditLogs)
        => new(
            new FakeRepository<Business>(new[] { business }),
            new FakeUnitOfWork(),
            TestCurrentUserService.For(AdminId, "PlatformAdmin"),
            new RecordingAuditSink(auditLogs),
            new TestAuditContext());

    private static GetBusinessDetailQueryHandler CreateGetBusinessDetailHandler(
        Business business,
        IEnumerable<Employee> employees,
        IEnumerable<Service> services,
        IEnumerable<Appointment> appointments)
        => new(
            new FakeRepository<Business>(new[] { business }),
            new FakeRepository<Employee>(employees),
            new FakeRepository<Service>(services),
            new FakeRepository<Appointment>(appointments));
}
