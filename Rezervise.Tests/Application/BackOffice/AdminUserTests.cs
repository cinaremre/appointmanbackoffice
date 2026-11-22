using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Users.Commands.LockAdminUser;
using Rezervise.Application.Features.Admin.Users.Commands.RevokeUserSessions;
using Rezervise.Application.Features.Admin.Users.Commands.UnlockAdminUser;
using Rezervise.Application.Features.Admin.Users.Queries.GetAdminUserDetail;
using Rezervise.Application.Features.Admin.Users.Queries.GetAdminUsers;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application.BackOffice;

/// <summary>
/// Backoffice kullanici yonetimi (dokuman Modul 11): kilitleme, acilis,
/// oturum iptali ve listeleme davranislari.
/// </summary>
public class AdminUserTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly DateTime NowUtc = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    // ===== Denetim kaydi fake'leri =====

    /// <summary>Denetim kaydinin yakalandigi bellek ici kayit.</summary>
    private sealed class AuditEntry
    {
        public Guid ActorUserId { get; set; }
        public string? ActorRole { get; set; }
        public Guid? BusinessId { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Action { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? Reason { get; set; }
        public string? CorrelationId { get; set; }
        public string? IpAddress { get; set; }
    }

    /// <summary>IAdminAuditSink fake'i: kayitlari listeye alir, hata uretmez.</summary>
    private sealed class FakeAdminAuditSink : IAdminAuditSink
    {
        public List<AuditEntry> Entries { get; } = new();

        public Task RecordAsync(
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
            Entries.Add(new AuditEntry
            {
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                BusinessId = businessId,
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                Reason = reason,
                CorrelationId = correlationId,
                IpAddress = ipAddress
            });

            return Task.CompletedTask;
        }
    }

    /// <summary>Sabit correlation ve IP ureten denetim baglami fake'i.</summary>
    private sealed class FakeAdminAuditContext : IAdminAuditContext
    {
        public string CorrelationId => "test-correlation";
        public string IpAddress => "127.0.0.1";
    }

    // ===== Yardimcilar =====

    private static TestCurrentUserService AdminUser()
        => TestCurrentUserService.For(AdminId, "PlatformAdmin");

    private static (FakeAdminAuditSink Sink, FakeAdminAuditContext Context) CreateAudit()
        => (new FakeAdminAuditSink(), new FakeAdminAuditContext());

    // ===== Kilitleme =====

    [Fact]
    public async Task Lock_WithEmptyReason_ShouldReturnValidationErrorWithoutAudit()
    {
        var user = TestData.NewUser(Guid.NewGuid());
        var (sink, context) = CreateAudit();
        var sut = new LockAdminUserCommandHandler(
            new FakeRepository<User>(new[] { user }),
            new FakeUnitOfWork(),
            AdminUser(),
            sink,
            context);

        var result = await sut.Handle(
            new LockAdminUserCommand { UserId = user.Id, Reason = "   " },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        Assert.Empty(sink.Entries);
        Assert.Equal(EnumAccountStatus.Active, user.AccountStatus);
    }

    [Fact]
    public async Task Lock_ShouldLockAccountAndRecordAudit()
    {
        var user = TestData.NewUser(Guid.NewGuid());
        var userRepo = new FakeRepository<User>(new[] { user });
        var (sink, context) = CreateAudit();
        var sut = new LockAdminUserCommandHandler(
            userRepo,
            new FakeUnitOfWork(),
            AdminUser(),
            sink,
            context);

        var result = await sut.Handle(
            new LockAdminUserCommand { UserId = user.Id, Reason = "Supheli aktivite" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EnumAccountStatus.Locked, user.AccountStatus);

        // Denetim kaydi: kilit eylemi, gerekce ve aktor kayit altina alinmali.
        var entry = Assert.Single(sink.Entries);
        Assert.Equal("LockUser", entry.Action);
        Assert.Equal("User", entry.EntityType);
        Assert.Equal(user.Id.ToString(), entry.EntityId);
        Assert.Equal(AdminId, entry.ActorUserId);
        Assert.Equal("PlatformAdmin", entry.ActorRole);
        Assert.Equal("Supheli aktivite", entry.Reason);
        Assert.Equal("test-correlation", entry.CorrelationId);
        Assert.Equal("127.0.0.1", entry.IpAddress);

        // Detay sorgusu kilit bayragini dogru bildirmeli.
        var detailHandler = new GetAdminUserDetailQueryHandler(
            userRepo,
            new FakeRepository<Business>(),
            new FakeRepository<Employee>());
        var detail = await detailHandler.Handle(
            new GetAdminUserDetailQuery { UserId = user.Id },
            CancellationToken.None);

        Assert.True(detail.IsSuccess);
        Assert.True(detail.Value!.IsLockedOut);
    }

    // ===== Kilit acma =====

    [Fact]
    public async Task Unlock_ShouldActivateAccountAndRecordAudit()
    {
        var user = TestData.NewUser(Guid.NewGuid());
        user.LockAccount("onceki kilit", "test");
        var (sink, context) = CreateAudit();
        var sut = new UnlockAdminUserCommandHandler(
            new FakeRepository<User>(new[] { user }),
            new FakeUnitOfWork(),
            AdminUser(),
            sink,
            context);

        var result = await sut.Handle(
            new UnlockAdminUserCommand { UserId = user.Id, Reason = "Itiraz kabul edildi" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EnumAccountStatus.Active, user.AccountStatus);

        var entry = Assert.Single(sink.Entries);
        Assert.Equal("UnlockUser", entry.Action);
        Assert.Equal("Itiraz kabul edildi", entry.Reason);
        Assert.Contains("Locked", entry.OldValues, StringComparison.Ordinal);
        Assert.Contains("Active", entry.NewValues, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unlock_WithEmptyReason_ShouldReturnValidationError()
    {
        var user = TestData.NewUser(Guid.NewGuid());
        var (sink, context) = CreateAudit();
        var sut = new UnlockAdminUserCommandHandler(
            new FakeRepository<User>(new[] { user }),
            new FakeUnitOfWork(),
            AdminUser(),
            sink,
            context);

        var result = await sut.Handle(
            new UnlockAdminUserCommand { UserId = user.Id, Reason = "" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        Assert.Empty(sink.Entries);
    }

    // ===== Oturum iptali =====

    [Fact]
    public async Task RevokeSessions_ShouldClearRefreshTokenHashesAndRecordAudit()
    {
        var user = TestData.NewUser(Guid.NewGuid());
        // Iki kez token veriliyor: ikincisi ilki PreviousRefreshTokenHash slotine tasir.
        user.IssueRefreshToken("hash-1", NowUtc.AddDays(7), NowUtc);
        user.IssueRefreshToken("hash-2", NowUtc.AddDays(7), NowUtc.AddMinutes(5));
        Assert.NotNull(user.RefreshTokenHash);
        Assert.NotNull(user.PreviousRefreshTokenHash);

        var (sink, context) = CreateAudit();
        var sut = new RevokeUserSessionsCommandHandler(
            new FakeRepository<User>(new[] { user }),
            new FakeUnitOfWork(),
            AdminUser(),
            new StubDateTimeService(NowUtc),
            sink,
            context);

        var result = await sut.Handle(
            new RevokeUserSessionsCommand { UserId = user.Id, Reason = "Destek talebi uzerine" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Hash alanlari null'a cekilmeli: token yenilenemez.
        Assert.Null(user.RefreshTokenHash);
        Assert.Null(user.PreviousRefreshTokenHash);
        Assert.Equal(NowUtc, user.RefreshTokenRevokedAtUtc);

        var entry = Assert.Single(sink.Entries);
        Assert.Equal("RevokeSessions", entry.Action);
        Assert.Equal("Destek talebi uzerine", entry.Reason);
        Assert.Equal(user.Id.ToString(), entry.EntityId);
        // Hash degerleri asla denetim kaydina yazilmaz.
        Assert.DoesNotContain("hash-1", entry.NewValues, StringComparison.Ordinal);
        Assert.DoesNotContain("hash-2", entry.NewValues, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RevokeSessions_WithEmptyReason_ShouldReturnValidationError()
    {
        var user = TestData.NewUser(Guid.NewGuid());
        user.IssueRefreshToken("hash-1", NowUtc.AddDays(7), NowUtc);
        var (sink, context) = CreateAudit();
        var sut = new RevokeUserSessionsCommandHandler(
            new FakeRepository<User>(new[] { user }),
            new FakeUnitOfWork(),
            AdminUser(),
            new StubDateTimeService(NowUtc),
            sink,
            context);

        var result = await sut.Handle(
            new RevokeUserSessionsCommand { UserId = user.Id, Reason = " " },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        Assert.NotNull(user.RefreshTokenHash);
        Assert.Empty(sink.Entries);
    }

    // ===== Listeleme =====

    [Fact]
    public async Task GetAdminUsers_ShouldFilterBySearchEmailAndStatus()
    {
        var zeynep = TestData.NewUser(Guid.NewGuid());
        zeynep.SetFirstName("Zeynep");
        zeynep.SetLastName("Kaya");

        var ali = TestData.NewUser(Guid.NewGuid());
        ali.SetFirstName("Ali");
        ali.SetLastName("Demir");

        var locked = TestData.NewUser(Guid.NewGuid());
        locked.SetFirstName("Mehmet");
        locked.SetLastName("Yilmaz");
        locked.LockAccount("test kilidi", "test");

        var sut = new GetAdminUsersQueryHandler(
            new FakeRepository<User>(new[] { zeynep, ali, locked }));

        // 1. Arama: yalnizca ad/soyad/e-posta alanlarinda "ali" gecen kullanici.
        var searchResult = await sut.Handle(
            new GetAdminUsersQuery { Search = "ali", Page = 1, PageSize = 50 },
            CancellationToken.None);
        AssertSearchResult(searchResult, ali.Id);

        // 2. E-posta filtresi.
        var emailResult = await sut.Handle(
            new GetAdminUsersQuery { Email = $"user-{ali.Id:N}@test.com", Page = 1, PageSize = 50 },
            CancellationToken.None);
        AssertSearchResult(emailResult, ali.Id);

        // 3. Durum filtresi: aktif olmayan hesaplar.
        var inactiveResult = await sut.Handle(
            new GetAdminUsersQuery { IsActive = false, Page = 1, PageSize = 50 },
            CancellationToken.None);
        AssertSearchResult(inactiveResult, locked.Id);

        // 4. Durum filtresi: aktif hesaplar.
        var activeResult = await sut.Handle(
            new GetAdminUsersQuery { IsActive = true, Page = 1, PageSize = 50 },
            CancellationToken.None);
        Assert.Equal(2, activeResult.Value!.TotalCount);
        Assert.All(activeResult.Value.Items, u => Assert.NotEqual(locked.Id, u.Id));
    }

    [Fact]
    public async Task GetAdminUsers_ShouldClampPagingValues()
    {
        var users = Enumerable.Range(0, 5).Select(_ => TestData.NewUser(Guid.NewGuid())).ToList();
        var sut = new GetAdminUsersQueryHandler(new FakeRepository<User>(users));

        // pageSize 200'un uzerinde istenirse en fazla 200'e cekilir.
        var oversized = await sut.Handle(
            new GetAdminUsersQuery { Page = 1, PageSize = 1000 },
            CancellationToken.None);
        Assert.True(oversized.IsSuccess);
        Assert.Equal(GetAdminUsersQueryHandler.MaxPageSize, oversized.Value!.PageSize);
        Assert.Equal(5, oversized.Value.TotalCount);

        // Sayfa numarasi en az 1'dir.
        var zeroPage = await sut.Handle(
            new GetAdminUsersQuery { Page = 0, PageSize = 0 },
            CancellationToken.None);
        Assert.True(zeroPage.IsSuccess);
        Assert.Equal(1, zeroPage.Value!.Page);
        Assert.Equal(GetAdminUsersQueryHandler.DefaultPageSize, zeroPage.Value.PageSize);
    }

    // ===== Detay =====

    [Fact]
    public async Task GetAdminUserDetail_ShouldReturnOwnedBusinessesAndEmployeePositions()
    {
        var owner = TestData.NewUser(Guid.NewGuid());
        var business = TestData.NewBusiness(owner.Id);
        var employee = TestData.NewEmployee(business.Id);
        employee.LinkToUser(owner);

        var sut = new GetAdminUserDetailQueryHandler(
            new FakeRepository<User>(new[] { owner }),
            new FakeRepository<Business>(new[] { business }),
            new FakeRepository<Employee>(new[] { employee }));

        var result = await sut.Handle(
            new GetAdminUserDetailQuery { UserId = owner.Id },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.Equal(owner.Id, dto.Id);

        var owned = Assert.Single(dto.OwnedBusinesses);
        Assert.Equal(business.Id, owned.Id);
        Assert.Equal("Test Isletmesi", owned.Name);

        var position = Assert.Single(dto.EmployeePositions);
        Assert.Equal(employee.Id, position.Id);
        Assert.Equal(business.Id, position.BusinessId);
    }

    [Fact]
    public async Task GetAdminUserDetail_WhenUserMissing_ShouldReturnNotFound()
    {
        var sut = new GetAdminUserDetailQueryHandler(
            new FakeRepository<User>(),
            new FakeRepository<Business>(),
            new FakeRepository<Employee>());

        var result = await sut.Handle(
            new GetAdminUserDetailQuery { UserId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    /// <summary>Filtrelenmis sonucun yalnizca beklenen kullaniciyi dondugunu dogrular.</summary>
    private static void AssertSearchResult(
        Result<AdminPagedResult<AdminUserListItemDto>> result,
        Guid expectedUserId)
    {
        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(expectedUserId, item.Id);
    }
}
