using Rezervise.Application.Features.Admin.Audit.Queries.GetAdminAuditLogs;
using Rezervise.Application.Features.Admin.Operations.Queries.GetAdminFailedOperations;
using Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobDetail;
using Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobs;
using Rezervise.Application.Features.Admin.Operations.Queries.GetSystemHealth;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application.BackOffice;

/// <summary>
/// Backoffice operasyon izleme (dokuman Modul 20) ve denetim kaydi okuma
/// (Modul 21): aktarim isi listesi/detayi, basarisiz operasyonlar, sistem
/// saglik raporu ve audit log filtre/siralama.
/// </summary>
public class AdminOperationsTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OtherBusinessId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid OtherActorId = Guid.NewGuid();

    // ===== Aktarim isi listesi =====

    [Fact]
    public async Task GetJobs_ShouldFilterByStatusAndBusiness()
    {
        var completed = NewJob(BusinessId);
        completed.StartProcessing();
        completed.Complete(1, 1);

        var failed = NewJob(OtherBusinessId);
        failed.MarkFailed();

        var uploaded = NewJob(BusinessId);

        var sut = new GetAdminJobsQueryHandler(
            new FakeRepository<ImportJob>(new[] { completed, failed, uploaded }));

        var byStatus = await sut.Handle(
            new GetAdminJobsQuery { Status = EnumImportStatus.Failed },
            CancellationToken.None);

        Assert.True(byStatus.IsSuccess);
        Assert.Equal(1, byStatus.Value!.TotalCount);
        var job = Assert.Single(byStatus.Value.Items);
        Assert.Equal(failed.Id, job.Id);
        Assert.Equal(EnumImportStatus.Failed, job.Status);
        Assert.Equal(OtherBusinessId, job.BusinessId);

        var byBusiness = await sut.Handle(
            new GetAdminJobsQuery { BusinessId = BusinessId },
            CancellationToken.None);

        Assert.True(byBusiness.IsSuccess);
        Assert.Equal(2, byBusiness.Value!.Items.Count);
        Assert.All(byBusiness.Value.Items, j => Assert.Equal(BusinessId, j.BusinessId));
    }

    [Fact]
    public async Task GetJobs_ShouldClampPageSize()
    {
        var sut = new GetAdminJobsQueryHandler(new FakeRepository<ImportJob>());

        var result = await sut.Handle(
            new GetAdminJobsQuery { Page = -3, PageSize = 999 },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Page);
        Assert.Equal(200, result.Value.PageSize);
    }

    // ===== Is detayi =====

    [Fact]
    public async Task GetJobDetail_ShouldReturnJobAndRows()
    {
        var job = NewJob(BusinessId);
        job.StartProcessing();

        var rows = new FakeRepository<ImportRowResult>(new[]
        {
            new ImportRowResult(job.Id, 3, EnumImportRowStatus.Pending),
            new ImportRowResult(job.Id, 1, EnumImportRowStatus.Imported, "905321112233"),
            new ImportRowResult(job.Id, 2, EnumImportRowStatus.Failed, "905322223344", "Gecersiz telefon")
        });

        var sut = new GetAdminJobDetailQueryHandler(
            new FakeRepository<ImportJob>(new[] { job }), rows);

        var result = await sut.Handle(
            new GetAdminJobDetailQuery { ImportJobId = job.Id },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(job.Id, result.Value!.Job.Id);
        Assert.Equal(EnumImportStatus.Processing, result.Value.Job.Status);
        Assert.Equal(job.TotalRows, result.Value.Job.TotalRows);

        // Satirlar satir numarasina gore siralanir.
        Assert.Equal(3, result.Value.Rows.Count);
        Assert.Equal(1, result.Value.Rows[0].RowNumber);
        Assert.Equal(2, result.Value.Rows[1].RowNumber);
        Assert.Equal(3, result.Value.Rows[2].RowNumber);
        Assert.Equal("Gecersiz telefon", result.Value.Rows[1].ErrorDetails);
    }

    [Fact]
    public async Task GetJobDetail_ShouldFailNotFound_WhenJobMissing()
    {
        var sut = new GetAdminJobDetailQueryHandler(
            new FakeRepository<ImportJob>(),
            new FakeRepository<ImportRowResult>());

        var result = await sut.Handle(
            new GetAdminJobDetailQuery { ImportJobId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    // ===== Basarisiz operasyonlar =====

    [Fact]
    public async Task GetFailedOperations_ShouldListFailedJobsAndRows()
    {
        var failed = NewJob(BusinessId, "excel");
        failed.MarkFailed();

        var completed = NewJob(BusinessId, "google-contacts");
        completed.StartProcessing();
        completed.Complete(2, 0);

        var rows = new FakeRepository<ImportRowResult>(new[]
        {
            new ImportRowResult(failed.Id, 1, EnumImportRowStatus.Failed, "905321112233", "Gecersiz isim"),
            new ImportRowResult(failed.Id, 2, EnumImportRowStatus.Failed, "905322223344", "Eksik alan"),
            new ImportRowResult(completed.Id, 1, EnumImportRowStatus.Imported, "905323335556")
        });

        var sut = new GetAdminFailedOperationsQueryHandler(
            new FakeRepository<ImportJob>(new[] { failed, completed }), rows);

        var result = await sut.Handle(new GetAdminFailedOperationsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var job = Assert.Single(result.Value!.FailedJobs);
        Assert.Equal(failed.Id, job.Id);
        Assert.Equal(EnumImportStatus.Failed, job.Status);

        Assert.Equal(2, result.Value.FailedRows.Count);
        Assert.All(result.Value.FailedRows, r => Assert.Equal(failed.Id, r.ImportJobId));
    }

    [Fact]
    public async Task GetFailedOperations_ShouldCapRowSamplesAtFifty()
    {
        var failed = NewJob(BusinessId);
        failed.MarkFailed();

        var rows = Enumerable.Range(1, 55)
            .Select(i => new ImportRowResult(
                failed.Id, i, EnumImportRowStatus.Failed, $"90532000{i:00}", "Ornek hata"))
            .ToList();

        var sut = new GetAdminFailedOperationsQueryHandler(
            new FakeRepository<ImportJob>(new[] { failed }),
            new FakeRepository<ImportRowResult>(rows));

        var result = await sut.Handle(new GetAdminFailedOperationsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value!.FailedRows.Count);

        // Ornekler satir numarasina gore ilk 50'dir.
        Assert.Equal(1, result.Value.FailedRows[0].RowNumber);
        Assert.Equal(50, result.Value.FailedRows[49].RowNumber);
    }

    // ===== Sistem sagligi =====

    [Fact]
    public async Task GetSystemHealth_ShouldCountJobStates()
    {
        var pending1 = NewJob(BusinessId);
        var pending2 = NewJob(BusinessId);
        pending2.MarkAwaitingMapping();

        var running = NewJob(BusinessId);
        running.StartProcessing();

        var failed = NewJob(OtherBusinessId);
        failed.MarkFailed();

        var completed = NewJob(BusinessId);
        completed.StartProcessing();
        completed.Complete(2, 0);

        var sut = new GetSystemHealthQueryHandler(
            new FakeRepository<ImportJob>(new[] { pending1, pending2, running, failed, completed }));

        var result = await sut.Handle(new GetSystemHealthQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Healthy", result.Value!.Status);
        Assert.Equal(2, result.Value.PendingJobs);
        Assert.Equal(1, result.Value.RunningJobs);
        Assert.Equal(1, result.Value.FailedJobs);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Note));
    }

    // ===== Denetim kayitlari =====

    [Fact]
    public async Task GetAuditLogs_ShouldFilterByActorAndBusiness()
    {
        var logs = new FakeRepository<AdminAuditLog>(new[]
        {
            NewLog(ActorId, BusinessId, t: 1),
            NewLog(ActorId, BusinessId, t: 2),
            NewLog(OtherActorId, OtherBusinessId, t: 3)
        });

        var sut = new GetAdminAuditLogsQueryHandler(logs);

        var byActor = await sut.Handle(
            new GetAdminAuditLogsQuery { ActorUserId = ActorId },
            CancellationToken.None);

        Assert.True(byActor.IsSuccess);
        Assert.Equal(2, byActor.Value!.TotalCount);
        Assert.All(byActor.Value.Items, l => Assert.Equal(ActorId, l.ActorUserId));

        var byBusiness = await sut.Handle(
            new GetAdminAuditLogsQuery { BusinessId = OtherBusinessId },
            CancellationToken.None);

        Assert.True(byBusiness.IsSuccess);
        var log = Assert.Single(byBusiness.Value!.Items);
        Assert.Equal(OtherActorId, log.ActorUserId);
        Assert.Equal(OtherBusinessId, log.BusinessId);
    }

    [Fact]
    public async Task GetAuditLogs_ShouldSortDescendingByCreatedAt()
    {
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var logs = new FakeRepository<AdminAuditLog>(new[]
        {
            NewLog(ActorId, BusinessId, t: 1, at: baseTime.AddHours(1)),
            NewLog(ActorId, BusinessId, t: 2, at: baseTime),
            NewLog(ActorId, BusinessId, t: 3, at: baseTime.AddHours(2))
        });

        var sut = new GetAdminAuditLogsQueryHandler(logs);

        var result = await sut.Handle(new GetAdminAuditLogsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.TotalCount);

        // En yeni kayit basta: CreatedAtUtc tersten sirali.
        Assert.Equal(baseTime.AddHours(2), result.Value.Items[0].CreatedAtUtc);
        Assert.Equal(baseTime.AddHours(1), result.Value.Items[1].CreatedAtUtc);
        Assert.Equal(baseTime, result.Value.Items[2].CreatedAtUtc);
    }

    // ===== Yardimcilar =====

    /// <summary>Aktarim isi uretir; domain event'leri temizlenir.</summary>
    private static ImportJob NewJob(Guid businessId, string sourceSystem = "excel")
    {
        var job = new ImportJob(
            businessId,
            sourceSystem,
            "musteriler.csv",
            "Ad,Phone\r\nAli,5321112233\r\nVeli,5322223344",
            2,
            Guid.NewGuid());

        job.ClearDomainEvents();
        return job;
    }

    /// <summary>Denetim kaydi uretir; t sadece EntityId'yi cesitlendirir.</summary>
    private static AdminAuditLog NewLog(Guid actorId, Guid? businessId, int t, DateTime? at = null)
        => new(
            actorId,
            "PlatformAdmin",
            businessId,
            "Business",
            $"entity-{t}",
            "Suspend",
            at ?? DateTime.UtcNow,
            oldValues: "{\"Status\":\"Active\"}",
            newValues: "{\"Status\":\"Suspended\"}",
            reason: $"Test gerekcesi {t}",
            correlationId: $"corr-{t}",
            ipAddress: "127.0.0.1");
}
