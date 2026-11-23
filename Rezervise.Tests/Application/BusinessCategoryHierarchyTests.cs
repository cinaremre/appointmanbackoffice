using System.Collections;
using System.Reflection;
using Rezervise.Application.Features.BusinessCategories.Queries.GetAll;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Infrastructure.Persistence.SeedData;
using Rezervise.Tests;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// Isletme kategorisi hiyerarsisi: 12 ana kategori, alt kategori baglantilari,
/// sayaclar ve mevcut 8 GUID'in korunmasi.
/// </summary>
public class BusinessCategoryHierarchyTests
{
    [Fact]
    public async Task GetAll_ShouldReturnRootsWithSubCategoriesAndCounts()
    {
        var ownerId = Guid.NewGuid();
        var root = new BusinessCategory(Guid.NewGuid(), "Güzellik ve Kişisel Bakım", 1);
        var child = new BusinessCategory(Guid.NewGuid(), "Kuaför", 1);
        root.AddSubCategory(child);

        var business = TestData.NewBusiness(ownerId);
        child.AddBusiness(business);

        var sut = new GetAllCategoriesQueryHandler(
            new FakeRepository<BusinessCategory>(new[] { root, child }));

        var result = await sut.Handle(new GetAllCategoriesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var single = Assert.Single(result.Value);
        Assert.Equal(root.Id, single.Id);
        Assert.Equal(1, single.SubCategoryCount);
        Assert.Single(single.SubCategories);
        Assert.Equal(child.Id, single.SubCategories[0].Id);
        Assert.Equal(1, single.SubCategories[0].BusinessCount);
    }

    [Fact]
    public async Task GetAll_ShouldFilterInactiveSubCategories_WhenOnlyActiveRequested()
    {
        var root = new BusinessCategory(Guid.NewGuid(), "Saglik ve Medikal", 1);
        var activeChild = new BusinessCategory(Guid.NewGuid(), "Dis Klinigi", 1);
        var inactiveChild = new BusinessCategory(Guid.NewGuid(), "Kapanmis Klinik", 2);
        root.AddSubCategory(activeChild);
        root.AddSubCategory(inactiveChild);
        inactiveChild.Deactivate();

        var sut = new GetAllCategoriesQueryHandler(
            new FakeRepository<BusinessCategory>(new[] { root, activeChild, inactiveChild }));

        var result = await sut.Handle(
            new GetAllCategoriesQuery { OnlyActive = true }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var single = Assert.Single(result.Value);
        Assert.Single(single.SubCategories);
        Assert.Equal(1, single.SubCategoryCount);
    }

    [Fact]
    public void SeedDefinition_ShouldContainTwelveRootCategories()
    {
        var nodes = BuildSeedNodes();

        var roots = nodes.Where(n => ParentIdOf(n) is null).ToList();

        Assert.Equal(12, roots.Count);
    }

    [Fact]
    public void SeedDefinition_ShouldGiveEveryRootAtLeastOneSubCategory()
    {
        var nodes = BuildSeedNodes();

        var roots = nodes.Where(n => ParentIdOf(n) is null).Select(IdOf).ToList();

        foreach (var rootId in roots)
        {
            Assert.Contains(nodes, n => ParentIdOf(n) == rootId);
        }
    }

    [Fact]
    public void SeedDefinition_ShouldPreserveLegacyCategoryIds()
    {
        var nodes = BuildSeedNodes();

        var legacyIds = new[]
        {
            BusinessCategorySeed.BarberId,
            BusinessCategorySeed.DentistId,
            BusinessCategorySeed.TattooId,
            BusinessCategorySeed.WaxingId,
            BusinessCategorySeed.HairdresserId,
            BusinessCategorySeed.NailSalonId,
            BusinessCategorySeed.SpaId,
            BusinessCategorySeed.MassageId
        };

        foreach (var legacyId in legacyIds)
            Assert.Contains(nodes, n => IdOf(n) == legacyId);
    }

    [Fact]
    public void SeedDefinition_ShouldKeepCorrespondingBusinessTypes()
    {
        var nodes = BuildSeedNodes();

        var barber = nodes.Single(n => IdOf(n) == BusinessCategorySeed.BarberId);
        var dentist = nodes.Single(n => IdOf(n) == BusinessCategorySeed.DentistId);

        Assert.Equal(EnumBusinessType.Barber, BusinessTypeOf(barber));
        Assert.Equal(EnumBusinessType.Dentist, BusinessTypeOf(dentist));
    }

    [Fact]
    public void SeedDefinition_ShouldAttachLegacyCategoriesToNewRoots()
    {
        var nodes = BuildSeedNodes();

        var barber = nodes.Single(n => IdOf(n) == BusinessCategorySeed.BarberId);
        var dentist = nodes.Single(n => IdOf(n) == BusinessCategorySeed.DentistId);

        Assert.Equal(BusinessCategorySeed.BeautyRootId, ParentIdOf(barber));
        Assert.Equal(BusinessCategorySeed.HealthRootId, ParentIdOf(dentist));
    }

    [Fact]
    public void SeedDefinition_ShouldHaveNoDuplicateIds()
    {
        var nodes = BuildSeedNodes();

        var duplicateIds = nodes
            .GroupBy(IdOf)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicateIds);
    }

    // Seed'in dugum listesi private; EF olmadan dogrulanabilmesi icin yansima kullanilir.
    private static List<object> BuildSeedNodes()
    {
        var method = typeof(BusinessCategorySeed)
            .GetMethod("BuildNodes", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BusinessCategorySeed.BuildNodes bulunamadi.");

        var result = method.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("BuildNodes sonucu okunamadi.");

        return result.Cast<object>().ToList();
    }

    private static Guid IdOf(object node) => (Guid)Property(node, "Id")!;

    private static Guid? ParentIdOf(object node) => (Guid?)Property(node, "ParentId");

    private static EnumBusinessType? BusinessTypeOf(object node)
        => (EnumBusinessType?)Property(node, "BusinessType");

    private static object? Property(object node, string name)
    {
        var property = node.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"'{name}' ozelligi bulunamadi.");

        return property.GetValue(node);
    }
}
