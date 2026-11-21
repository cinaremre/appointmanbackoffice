using AppointMan.Application.Features.Admin.Services.Commands.CancelBulkServiceOperation;
using AppointMan.Application.Features.Admin.Services.Commands.ExecuteBulkServiceOperation;
using AppointMan.Application.Features.Admin.Services.Commands.UpdateServiceByAdmin;
using AppointMan.Application.Features.Admin.Services.Queries.PreviewBulkServiceOperation;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application.BackOffice;

/// <summary>
/// Hizmet katalogu ve toplu islemler (dokuman Modul 10): admin guncellemesi,
/// onizleme ayristirmasi, yurutme ve iptal durum kurallari.
/// </summary>
public class AdminBulkOperationTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid BusinessId = Guid.NewGuid();

    private static (PreviewBulkServiceOperationQueryHandler Handler, FakeRepository<BulkOperation> BulkRepo)
        NewPreviewHandler(FakeRepository<Service> services, FakeRepository<ServiceCategory>? categories = null)
    {
        var bulkRepo = new FakeRepository<BulkOperation>();
        var handler = new PreviewBulkServiceOperationQueryHandler(
            services,
            categories ?? new FakeRepository<ServiceCategory>(),
            bulkRepo,
            new FakeUnitOfWork(),
            TestCurrentUserService.For(AdminId, "PlatformAdmin"),
            new TestAdminAuditSink(),
            new TestAdminAuditContext());

        return (handler, bulkRepo);
    }

    private static (ExecuteBulkServiceOperationCommandHandler Handler, TestAdminAuditSink Sink)
        NewExecuteHandler(
            FakeRepository<BulkOperation> bulkRepo,
            FakeRepository<Service> services,
            FakeRepository<ServiceCategory>? categories = null)
    {
        var sink = new TestAdminAuditSink();
        var handler = new ExecuteBulkServiceOperationCommandHandler(
            bulkRepo,
            services,
            categories ?? new FakeRepository<ServiceCategory>(),
            new FakeUnitOfWork(),
            TestCurrentUserService.For(AdminId, "PlatformAdmin"),
            sink,
            new TestAdminAuditContext());

        return (handler, sink);
    }

    private static (CancelBulkServiceOperationCommandHandler Handler, TestAdminAuditSink Sink)
        NewCancelHandler(FakeRepository<BulkOperation> bulkRepo)
    {
        var sink = new TestAdminAuditSink();
        var handler = new CancelBulkServiceOperationCommandHandler(
            bulkRepo,
            new FakeUnitOfWork(),
            TestCurrentUserService.For(AdminId, "PlatformAdmin"),
            sink,
            new TestAdminAuditContext());

        return (handler, sink);
    }

    private static Service NewActiveService()
        => TestData.NewService(BusinessId, Guid.NewGuid());

    [Fact]
    public async Task UpdateServiceByAdmin_ShouldChangePriceAndAudit()
    {
        var serviceId = Guid.NewGuid();
        var service = TestData.NewService(BusinessId, serviceId);
        var services = new FakeRepository<Service>(new[] { service });

        var sink = new TestAdminAuditSink();
        var sut = new UpdateServiceByAdminCommandHandler(
            services,
            new FakeUnitOfWork(),
            TestCurrentUserService.For(AdminId, "PlatformAdmin"),
            sink,
            new TestAdminAuditContext());

        var result = await sut.Handle(
            new UpdateServiceByAdminCommand { ServiceId = serviceId, Price = 250m },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(250m, service.Price!.Amount);

        var entry = Assert.Single(sink.Entries);
        Assert.Equal("UpdateService", entry.Action);
        Assert.Equal("Service", entry.EntityType);
        Assert.Equal(BusinessId, entry.BusinessId);
        Assert.Contains("100", entry.OldValues);
        Assert.Contains("250", entry.NewValues);
    }

    [Fact]
    public async Task Preview_WithThreeValidAndOneInvalidId_ShouldReportCounts()
    {
        var s1 = NewActiveService();
        var s2 = NewActiveService();
        var s3 = NewActiveService();
        var invalidId = Guid.NewGuid();

        var services = new FakeRepository<Service>(new[] { s1, s2, s3 });
        var (sut, bulkRepo) = NewPreviewHandler(services);

        var result = await sut.Handle(
            new PreviewBulkServiceOperationQuery
            {
                OperationType = EnumBulkOperationType.DeactivateServices,
                ServiceIds = new List<Guid> { s1.Id, s2.Id, s3.Id, invalidId },
                DryRun = true
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.TotalRecords);
        var invalid = Assert.Single(result.Value.InvalidIds);
        Assert.Equal(invalidId, invalid);
        Assert.Equal(EnumBulkOperationStatus.Validating, result.Value.Status);
        Assert.True(result.Value.DryRun);

        // Kayit append-only payload ile saklanir.
        var operation = Assert.Single(bulkRepo.Items);
        Assert.True(operation.DryRun);
        Assert.Equal(3, operation.TotalRecords);
    }

    [Fact]
    public async Task Preview_WithMoreThanTwoThousandRecords_ShouldReturnConflict()
    {
        var services = new FakeRepository<Service>();
        var (sut, _) = NewPreviewHandler(services);

        var ids = Enumerable.Range(0, 2001).Select(_ => Guid.NewGuid()).ToList();

        var result = await sut.Handle(
            new PreviewBulkServiceOperationQuery
            {
                OperationType = EnumBulkOperationType.DeactivateServices,
                ServiceIds = ids
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
    }

    [Fact]
    public async Task Execute_OnDryRunRecord_ShouldReturnConflict()
    {
        var s1 = NewActiveService();
        var services = new FakeRepository<Service>(new[] { s1 });
        var (preview, bulkRepo) = NewPreviewHandler(services);

        var previewResult = await preview.Handle(
            new PreviewBulkServiceOperationQuery
            {
                OperationType = EnumBulkOperationType.DeactivateServices,
                ServiceIds = new List<Guid> { s1.Id },
                DryRun = true
            },
            CancellationToken.None);

        var (sut, _) = NewExecuteHandler(bulkRepo, services);

        var result = await sut.Handle(
            new ExecuteBulkServiceOperationCommand { BulkOperationId = previewResult.Value!.Id },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        Assert.True(s1.IsActive);
    }

    [Fact]
    public async Task Execute_DeactivateTwoServices_ShouldSucceedAndAudit()
    {
        var s1 = NewActiveService();
        var s2 = NewActiveService();
        var services = new FakeRepository<Service>(new[] { s1, s2 });
        var (preview, bulkRepo) = NewPreviewHandler(services);

        var previewResult = await preview.Handle(
            new PreviewBulkServiceOperationQuery
            {
                OperationType = EnumBulkOperationType.DeactivateServices,
                ServiceIds = new List<Guid> { s1.Id, s2.Id },
                DryRun = false
            },
            CancellationToken.None);

        var (sut, sink) = NewExecuteHandler(bulkRepo, services);

        var result = await sut.Handle(
            new ExecuteBulkServiceOperationCommand { BulkOperationId = previewResult.Value!.Id },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.SuccessCount);
        Assert.Equal(0, result.Value.FailedCount);
        Assert.Equal(EnumBulkOperationStatus.Completed, result.Value.Status);

        // Hizmetler pasif; kayit Completed durumunda.
        Assert.False(s1.IsActive);
        Assert.False(s2.IsActive);
        var operation = Assert.Single(bulkRepo.Items);
        Assert.Equal(EnumBulkOperationStatus.Completed, operation.Status);

        // Denetim kaydi: preview + execute yazildi.
        Assert.Contains(sink.Entries, e => e.Action == "Execute" && e.EntityType == "BulkOperation");
    }

    [Fact]
    public async Task Execute_SecondTimeAfterCompletion_ShouldReturnConflict()
    {
        var s1 = NewActiveService();
        var services = new FakeRepository<Service>(new[] { s1 });
        var (preview, bulkRepo) = NewPreviewHandler(services);

        var previewResult = await preview.Handle(
            new PreviewBulkServiceOperationQuery
            {
                OperationType = EnumBulkOperationType.DeactivateServices,
                ServiceIds = new List<Guid> { s1.Id },
                DryRun = false
            },
            CancellationToken.None);

        var (sut, _) = NewExecuteHandler(bulkRepo, services);

        var first = await sut.Handle(
            new ExecuteBulkServiceOperationCommand { BulkOperationId = previewResult.Value!.Id },
            CancellationToken.None);

        Assert.True(first.IsSuccess);

        var second = await sut.Handle(
            new ExecuteBulkServiceOperationCommand { BulkOperationId = previewResult.Value!.Id },
            CancellationToken.None);

        Assert.True(second.IsFailure);
        Assert.Equal("CONFLICT", second.Error!.Code);
    }

    [Fact]
    public async Task Execute_WithInvalidServiceId_ShouldReportPartialCompletion()
    {
        var s1 = NewActiveService();
        var services = new FakeRepository<Service>(new[] { s1 });
        var (preview, bulkRepo) = NewPreviewHandler(services);

        // Gecerli 1 + depoda olmayan 1: payload tum istenen ID'leri tasidigi
        // icin yurutme sirasinda bulunamayan kayit basarisiz sayilir.
        var s2 = NewActiveService();
        var previewResult = await preview.Handle(
            new PreviewBulkServiceOperationQuery
            {
                OperationType = EnumBulkOperationType.DeactivateServices,
                ServiceIds = new List<Guid> { s1.Id, s2.Id },
                DryRun = false
            },
            CancellationToken.None);

        var (sut, _) = NewExecuteHandler(bulkRepo, services);

        var result = await sut.Handle(
            new ExecuteBulkServiceOperationCommand { BulkOperationId = previewResult.Value!.Id },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.SuccessCount);
        Assert.Equal(1, result.Value.FailedCount);
        Assert.Equal(EnumBulkOperationStatus.PartiallyCompleted, result.Value.Status);
        Assert.False(s1.IsActive);
        Assert.NotNull(result.Value.ErrorReport);
    }

    [Fact]
    public async Task Execute_ChangeServiceCategory_ShouldMoveServicesToTargetCategory()
    {
        var s1 = NewActiveService();
        var s2 = NewActiveService();
        var services = new FakeRepository<Service>(new[] { s1, s2 });

        var targetCategory = new ServiceCategory(Guid.NewGuid(), 1);
        var categories = new FakeRepository<ServiceCategory>(new[] { targetCategory });

        var (preview, bulkRepo) = NewPreviewHandler(services, categories);

        var previewResult = await preview.Handle(
            new PreviewBulkServiceOperationQuery
            {
                OperationType = EnumBulkOperationType.ChangeServiceCategory,
                ServiceIds = new List<Guid> { s1.Id, s2.Id },
                TargetCategoryId = targetCategory.Id,
                DryRun = false
            },
            CancellationToken.None);

        var (sut, _) = NewExecuteHandler(bulkRepo, services, categories);

        var result = await sut.Handle(
            new ExecuteBulkServiceOperationCommand { BulkOperationId = previewResult.Value!.Id },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.SuccessCount);
        Assert.Equal(targetCategory.Id, s1.ServiceCategoryId);
        Assert.Equal(targetCategory.Id, s2.ServiceCategoryId);
    }

    [Fact]
    public async Task Cancel_FromValidating_ShouldSucceedAndAudit()
    {
        var s1 = NewActiveService();
        var services = new FakeRepository<Service>(new[] { s1 });
        var (preview, bulkRepo) = NewPreviewHandler(services);

        var previewResult = await preview.Handle(
            new PreviewBulkServiceOperationQuery
            {
                OperationType = EnumBulkOperationType.DeactivateServices,
                ServiceIds = new List<Guid> { s1.Id },
                DryRun = false
            },
            CancellationToken.None);

        var (sut, sink) = NewCancelHandler(bulkRepo);

        var result = await sut.Handle(
            new CancelBulkServiceOperationCommand { BulkOperationId = previewResult.Value!.Id },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EnumBulkOperationStatus.Cancelled, result.Value!.Status);

        var operation = Assert.Single(bulkRepo.Items);
        Assert.Equal(EnumBulkOperationStatus.Cancelled, operation.Status);
        Assert.True(s1.IsActive);

        var entry = Assert.Single(sink.Entries);
        Assert.Equal("Cancel", entry.Action);
    }

    [Fact]
    public async Task Cancel_FromRunning_ShouldReturnConflict()
    {
        // Islem kosarken iptal edilmez: entity dogrudan Running'a gecirilir.
        var operation = new BulkOperation(
            Guid.NewGuid(),
            EnumBulkOperationType.DeactivateServices,
            "{}",
            dryRun: false,
            AdminId,
            totalRecords: 0);

        operation.MarkValidating();
        operation.Start();

        var bulkRepo = new FakeRepository<BulkOperation>(new[] { operation });
        var (sut, _) = NewCancelHandler(bulkRepo);

        var result = await sut.Handle(
            new CancelBulkServiceOperationCommand { BulkOperationId = operation.Id },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        Assert.Equal(EnumBulkOperationStatus.Running, operation.Status);
    }
}
