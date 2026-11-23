using Rezervise.Application.Features.Imports.Commands.CreateImportJob;
using Rezervise.Application.Features.Imports.Commands.ExecuteImportJob;
using Rezervise.Application.Features.Imports.Queries.GetImportJobResult;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// CSV aktarim akisi: create (parse + Mapping), execute (telefon anahtari ile
/// mukerrer atlama) ve sonuc sorgusu. Tamamlanmis is tekrar calistirilamaz.
/// </summary>
public class ImportTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OtherBusinessId = Guid.NewGuid();

    private const string PhoneMapping = "{\"0\":\"FullName\",\"1\":\"Phone\"}";

    // ===== Is olusturma =====

    [Fact]
    public async Task CreateJob_ShouldCountRows_AndWaitForMapping()
    {
        var (sut, jobs, _) = CreateCreateSut();

        var result = await sut.Handle(new CreateImportJobCommand
        {
            BusinessId = BusinessId,
            SourceSystem = "excel",
            FileName = "musteriler.csv",
            CsvContent = "Ad,Phone\r\nAli,5321112233\r\nVeli,5322223344"
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var job = Assert.Single(jobs.Items);
        Assert.Equal(2, job.TotalRows);
        Assert.Equal(EnumImportStatus.Mapping, job.Status);
        Assert.Equal(OwnerId, job.CreatedBy);
    }

    [Fact]
    public async Task CreateJob_ShouldFail_WhenCsvEmpty()
    {
        var (sut, jobs, _) = CreateCreateSut();

        var result = await sut.Handle(new CreateImportJobCommand
        {
            BusinessId = BusinessId,
            SourceSystem = "excel",
            FileName = "musteriler.csv",
            CsvContent = "   "
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
        Assert.Empty(jobs.Items);
    }

    [Fact]
    public async Task CreateJob_ShouldFailUnauthorized_WhenCallerIsNotOwner()
    {
        var (sut, jobs, _) = CreateCreateSut(currentUser: TestCurrentUserService.For(Guid.NewGuid()));

        var result = await sut.Handle(new CreateImportJobCommand
        {
            BusinessId = BusinessId,
            SourceSystem = "excel",
            FileName = "musteriler.csv",
            CsvContent = "Ad,Phone\r\nAli,5321112233"
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
        Assert.Empty(jobs.Items);
    }

    // ===== Isletme (execute) =====

    [Fact]
    public async Task Execute_SamePhoneTwice_ShouldImportOnceAndSkipDuplicate()
    {
        // Iki farkli yazim ayni normalize edilmis telefonu uretmeli:
        // "5321112233" ve "+905321112233" -> ikisi de +905321112233 olur.
        var csv = "Ad,Phone\r\nAli,5321112233\r\nVeli,+905321112233";
        var (createSut, jobs, _) = CreateCreateSut();
        var jobId = (await createSut.Handle(NewCreateCommand(csv), CancellationToken.None)).Value;

        var rowResults = new FakeRepository<ImportRowResult>();
        var executeSut = CreateExecuteSut(jobs, rowResults);

        var result = await executeSut.Handle(NewExecuteCommand(jobId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var job = jobs.Items[0];
        Assert.Equal(EnumImportStatus.Completed, job.Status);
        Assert.Equal(1, job.ImportedRows);
        Assert.Equal(0, job.FailedRows);

        var imported = Assert.Single(rowResults.Items.Where(r => r.Status == EnumImportRowStatus.Imported));
        var skipped = Assert.Single(rowResults.Items.Where(r => r.Status == EnumImportRowStatus.SkippedDuplicate));
        Assert.Equal("+905321112233", imported.SourceRecordKey);
        Assert.Equal("+905321112233", skipped.SourceRecordKey);
        Assert.Equal(2, Math.Max(imported.RowNumber, skipped.RowNumber));
    }

    [Fact]
    public async Task Execute_InvalidPhone_ShouldMarkRowFailed()
    {
        var csv = "Ad,Phone\r\nAli,telefonum-degil";
        var (createSut, jobs, _) = CreateCreateSut();
        var jobId = (await createSut.Handle(NewCreateCommand(csv), CancellationToken.None)).Value;

        var rowResults = new FakeRepository<ImportRowResult>();
        var executeSut = CreateExecuteSut(jobs, rowResults);

        var result = await executeSut.Handle(NewExecuteCommand(jobId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, jobs.Items[0].FailedRows);

        var failed = Assert.Single(rowResults.Items.Where(r => r.Status == EnumImportRowStatus.Failed));
        Assert.NotNull(failed.ErrorDetails);
        Assert.Contains("Geçersiz telefon", failed.ErrorDetails);
    }

    [Fact]
    public async Task Execute_SecondRun_ShouldReturnConflict()
    {
        var csv = "Ad,Phone\r\nAli,5321112233";
        var (createSut, jobs, _) = CreateCreateSut();
        var jobId = (await createSut.Handle(NewCreateCommand(csv), CancellationToken.None)).Value;

        var executeSut = CreateExecuteSut(jobs, new FakeRepository<ImportRowResult>());

        var first = await executeSut.Handle(NewExecuteCommand(jobId), CancellationToken.None);
        var second = await executeSut.Handle(NewExecuteCommand(jobId), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("CONFLICT", second.Error!.Code);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenMappingHasNoPhone()
    {
        var csv = "Ad,Phone\r\nAli,5321112233";
        var (createSut, jobs, _) = CreateCreateSut();
        var jobId = (await createSut.Handle(NewCreateCommand(csv), CancellationToken.None)).Value;

        var executeSut = CreateExecuteSut(jobs, new FakeRepository<ImportRowResult>());

        var result = await executeSut.Handle(new ExecuteImportJobCommand
        {
            ImportJobId = jobId,
            ColumnMapping = "{\"0\":\"FullName\"}"
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
    }

    [Fact]
    public async Task Execute_ShouldSkipPhone_ImportedInPreviousJob()
    {
        // Onceki islerden ayni isletmeye ait Imported satir yeni iste engel olmali.
        var rowResults = new FakeRepository<ImportRowResult>();
        var jobs = new FakeRepository<ImportJob>();

        // 1. is: telefon yeni.
        var createSut = CreateCreateSut(jobs).Sut;
        var firstJobId = (await createSut.Handle(NewCreateCommand("Ad,Phone\r\nAli,5321112233"), CancellationToken.None)).Value;
        await CreateExecuteSut(jobs, rowResults).Handle(NewExecuteCommand(firstJobId), CancellationToken.None);

        // 2. is: ayni telefon tekrar.
        var secondJobId = (await createSut.Handle(NewCreateCommand("Ad,Phone\r\nMusteri,+905321112233"), CancellationToken.None)).Value;
        await CreateExecuteSut(jobs, rowResults).Handle(NewExecuteCommand(secondJobId), CancellationToken.None);

        Assert.Equal(1, jobs.Items[0].ImportedRows);
        Assert.Equal(0, jobs.Items[1].ImportedRows);

        var skippedSecondJob = rowResults.Items
            .Single(r => r.ImportJobId == secondJobId && r.Status == EnumImportRowStatus.SkippedDuplicate);
        Assert.Equal("+905321112233", skippedSecondJob.SourceRecordKey);
    }

    // ===== Sonuc sorgusu =====

    [Fact]
    public async Task GetResult_ShouldReturnSummaryAndRowStatuses()
    {
        var csv = "Ad,Phone\r\nAli,5321112233\r\nAli,5321112233\r\nVeli,telefonum-degil";
        var (createSut, jobs, _) = CreateCreateSut();
        var jobId = (await createSut.Handle(NewCreateCommand(csv), CancellationToken.None)).Value;

        var rowResults = new FakeRepository<ImportRowResult>();
        await CreateExecuteSut(jobs, rowResults).Handle(NewExecuteCommand(jobId), CancellationToken.None);

        var resultSut = new GetImportJobResultQueryHandler(jobs, rowResults, BusinessRepo(), CurrentUser());

        var result = await resultSut.Handle(new GetImportJobResultQuery { ImportJobId = jobId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal(EnumImportStatus.Completed, dto.Status);
        Assert.Equal(3, dto.TotalRows);
        Assert.Equal(1, dto.ImportedRows);
        Assert.Equal(1, dto.FailedRows);
        Assert.Equal(1, dto.SkippedRows);
        Assert.Equal(3, dto.Rows.Count);
        Assert.Contains(dto.Rows, r => r.Status == EnumImportRowStatus.Imported);
        Assert.Contains(dto.Rows, r => r.Status == EnumImportRowStatus.SkippedDuplicate);
        Assert.Contains(dto.Rows, r => r.Status == EnumImportRowStatus.Failed);
    }

    [Fact]
    public async Task GetResult_ShouldFailUnauthorized_WhenCallerIsNotOwner()
    {
        var resultSut = new GetImportJobResultQueryHandler(
            new FakeRepository<ImportJob>(),
            new FakeRepository<ImportRowResult>(),
            BusinessRepo(),
            TestCurrentUserService.For(Guid.NewGuid()));

        var result = await resultSut.Handle(new GetImportJobResultQuery { ImportJobId = Guid.NewGuid() }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    // ===== Yardimcilar =====

    private static TestCurrentUserService CurrentUser()
        => TestCurrentUserService.For(OwnerId);

    private static FakeRepository<Business> BusinessRepo()
        => new(new[]
        {
            TestData.NewBusiness(OwnerId, BusinessId),
            TestData.NewBusiness(Guid.NewGuid(), OtherBusinessId)
        });

    private static (CreateImportJobCommandHandler Sut, FakeRepository<ImportJob> Jobs, FakeUnitOfWork UnitOfWork)
        CreateCreateSut(FakeRepository<ImportJob>? jobs = null, TestCurrentUserService? currentUser = null)
    {
        var jobRepo = jobs ?? new FakeRepository<ImportJob>();
        var unitOfWork = new FakeUnitOfWork();

        return (
            new CreateImportJobCommandHandler(jobRepo, BusinessRepo(), unitOfWork, currentUser ?? CurrentUser()),
            jobRepo,
            unitOfWork);
    }

    private static ExecuteImportJobCommandHandler CreateExecuteSut(
        FakeRepository<ImportJob> jobs,
        FakeRepository<ImportRowResult> rowResults,
        TestCurrentUserService? currentUser = null)
        => new(
            jobs,
            rowResults,
            BusinessRepo(),
            new FakeUnitOfWork(),
            currentUser ?? CurrentUser());

    private static CreateImportJobCommand NewCreateCommand(string csv) => new()
    {
        BusinessId = BusinessId,
        SourceSystem = "excel",
        FileName = "musteriler.csv",
        CsvContent = csv
    };

    private static ExecuteImportJobCommand NewExecuteCommand(Guid jobId) => new()
    {
        ImportJobId = jobId,
        ColumnMapping = PhoneMapping
    };
}
