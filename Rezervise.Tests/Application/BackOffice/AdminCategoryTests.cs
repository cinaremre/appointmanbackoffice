using Rezervise.Application.Features.Admin.Categories.Commands.ArchiveBusinessCategory;
using Rezervise.Application.Features.Admin.Categories.Commands.CreateBusinessCategoryByAdmin;
using Rezervise.Application.Features.Admin.Categories.Commands.MoveBusinessCategory;
using Rezervise.Application.Features.Admin.Categories.Queries.GetBusinessCategoryTree;
using Rezervise.Application.Features.Admin.Categories.Queries.GetBusinessCategoryUsage;
using Rezervise.Domain.Entities;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application.BackOffice;

/// <summary>
/// Kategori yonetimi (dokuman Modul 9): agac listesi, karde ad cakismasi,
/// dongu korumasi ve arsivleme kurallari.
/// </summary>
public class AdminCategoryTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    private static CreateBusinessCategoryByAdminCommandHandler NewCreateHandler(
        FakeRepository<BusinessCategory> categories,
        TestAdminAuditSink audit,
        FakeUnitOfWork? unitOfWork = null)
        => new(
            categories,
            unitOfWork ?? new FakeUnitOfWork(),
            TestCurrentUserService.For(AdminId, "PlatformAdmin"),
            audit,
            new TestAdminAuditContext());

    private static ArchiveBusinessCategoryCommandHandler NewArchiveHandler(
        FakeRepository<BusinessCategory> categories,
        TestAdminAuditSink audit)
        => new(
            categories,
            new FakeUnitOfWork(),
            TestCurrentUserService.For(AdminId, "PlatformAdmin"),
            audit,
            new TestAdminAuditContext());

    private static MoveBusinessCategoryCommandHandler NewMoveHandler(
        FakeRepository<BusinessCategory> categories,
        TestAdminAuditSink audit)
        => new(
            categories,
            new FakeUnitOfWork(),
            TestCurrentUserService.For(AdminId, "PlatformAdmin"),
            audit,
            new TestAdminAuditContext());

    [Fact]
    public async Task GetTree_ShouldReturnFlatListWithParentAndChildCount()
    {
        var root = new BusinessCategory("Guzellik ve Kisisel Bakim", 1);
        var child = new BusinessCategory("Kuafor", 1);
        root.AddSubCategory(child);

        var sut = new GetBusinessCategoryTreeQueryHandler(
            new FakeRepository<BusinessCategory>(new[] { root, child }));

        var result = await sut.Handle(new GetBusinessCategoryTreeQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);

        var childNode = result.Value.Single(n => n.Id == child.Id);
        Assert.Equal(root.Id, childNode.ParentId);
        Assert.True(childNode.IsActive);

        var rootNode = result.Value.Single(n => n.Id == root.Id);
        Assert.Null(rootNode.ParentId);
        Assert.Equal(1, rootNode.ChildCount);
    }

    [Fact]
    public async Task Create_WithSameNameActiveSibling_ShouldReturnConflict()
    {
        var root = new BusinessCategory("Guzellik ve Kisisel Bakim", 1);
        var kuafor = new BusinessCategory("Kuafor", 1);
        root.AddSubCategory(kuafor);

        var categories = new FakeRepository<BusinessCategory>(new[] { root, kuafor });
        var sink = new TestAdminAuditSink();
        var sut = NewCreateHandler(categories, sink);

        var result = await sut.Handle(
            new CreateBusinessCategoryByAdminCommand
            {
                Name = "Kuafor",
                ParentId = root.Id,
                DisplayOrder = 2
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task Create_WithNewName_ShouldSucceedAndAudit()
    {
        var root = new BusinessCategory("Guzellik ve Kisisel Bakim", 1);
        var categories = new FakeRepository<BusinessCategory>(new[] { root });
        var sink = new TestAdminAuditSink();
        var sut = NewCreateHandler(categories, sink);

        var result = await sut.Handle(
            new CreateBusinessCategoryByAdminCommand
            {
                Name = "Berber",
                ParentId = root.Id,
                DisplayOrder = 2
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = categories.Items.Single(c => c.Name == "Berber");
        Assert.Equal(root.Id, created.ParentCategoryId);

        var entry = Assert.Single(sink.Entries);
        Assert.Equal("Admin", entry.ActorRole);
        Assert.Equal("BusinessCategory", entry.EntityType);
        Assert.Equal("Create", entry.Action);
        Assert.Equal(AdminId, entry.ActorUserId);
    }

    [Fact]
    public async Task Move_IntoOwnDescendant_ShouldReturnConflict()
    {
        // A, B'nin cocugu: A altina B tasinirsa dongu olusur (dokuman 9.3).
        var b = new BusinessCategory("Saglik ve Medikal", 1);
        var a = new BusinessCategory("Dis Klinigi", 1);
        b.AddSubCategory(a);

        var categories = new FakeRepository<BusinessCategory>(new[] { a, b });
        var sink = new TestAdminAuditSink();
        var sut = NewMoveHandler(categories, sink);

        var result = await sut.Handle(
            new MoveBusinessCategoryCommand { CategoryId = b.Id, TargetParentId = a.Id },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        // Dongu engellendi: B hala kok, A hala B'nin cocugu.
        Assert.Null(b.ParentCategoryId);
        Assert.Equal(b.Id, a.ParentCategoryId);
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task Move_ToRoot_ShouldDetachParent()
    {
        var b = new BusinessCategory("Saglik ve Medikal", 1);
        var a = new BusinessCategory("Dis Klinigi", 1);
        b.AddSubCategory(a);

        var categories = new FakeRepository<BusinessCategory>(new[] { a, b });
        var sink = new TestAdminAuditSink();
        var sut = NewMoveHandler(categories, sink);

        var result = await sut.Handle(
            new MoveBusinessCategoryCommand { CategoryId = a.Id, TargetParentId = null },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(a.ParentCategoryId);

        Assert.Single(sink.Entries);
        Assert.Equal("Move", sink.Entries[0].Action);
    }

    [Fact]
    public async Task Archive_WithEmptyReason_ShouldReturnValidation()
    {
        var category = new BusinessCategory("Guzellik ve Kisisel Bakim", 1);
        var categories = new FakeRepository<BusinessCategory>(new[] { category });
        var sink = new TestAdminAuditSink();
        var sut = NewArchiveHandler(categories, sink);

        var result = await sut.Handle(
            new ArchiveBusinessCategoryCommand { CategoryId = category.Id, Reason = "  " },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        Assert.True(category.IsActive);
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task Archive_WithActiveChild_ShouldReturnConflict()
    {
        var root = new BusinessCategory("Guzellik ve Kisisel Bakim", 1);
        var child = new BusinessCategory("Kuafor", 1);
        root.AddSubCategory(child);

        var categories = new FakeRepository<BusinessCategory>(new[] { root, child });
        var sink = new TestAdminAuditSink();
        var sut = NewArchiveHandler(categories, sink);

        var result = await sut.Handle(
            new ArchiveBusinessCategoryCommand
            {
                CategoryId = root.Id,
                Reason = "Kullanimda degil"
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        Assert.True(root.IsActive);
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task Archive_WithoutActiveChildren_ShouldDeactivateAndAudit()
    {
        var category = new BusinessCategory("Saglik ve Medikal", 1);
        var categories = new FakeRepository<BusinessCategory>(new[] { category });
        var sink = new TestAdminAuditSink();
        var sut = NewArchiveHandler(categories, sink);

        var result = await sut.Handle(
            new ArchiveBusinessCategoryCommand
            {
                CategoryId = category.Id,
                Reason = "Kategori artik kullanilmiyor"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(category.IsActive);

        var entry = Assert.Single(sink.Entries);
        Assert.Equal("Archive", entry.Action);
        Assert.Equal("Kategori artik kullanilmiyor", entry.Reason);
    }

    [Fact]
    public async Task Usage_ShouldCountActiveBusinessesWithCategory()
    {
        var category = new BusinessCategory("Guzellik ve Kisisel Bakim", 1);
        var ownerId = Guid.NewGuid();

        var withCategory = TestData.NewBusiness(ownerId);
        withCategory.SetCategory(category);

        var withoutCategory = TestData.NewBusiness(ownerId);

        var businesses = new FakeRepository<Business>(new[] { withCategory, withoutCategory });
        var sut = new GetBusinessCategoryUsageQueryHandler(
            new FakeRepository<BusinessCategory>(new[] { category }),
            businesses);

        var result = await sut.Handle(
            new GetBusinessCategoryUsageQuery { CategoryId = category.Id },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.BusinessCount);
        Assert.Equal(category.Name, result.Value.Name);
    }
}
