using System.Reflection;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Infrastructure.Persistence.SeedData;

namespace Rezervise.Tests.Application;

/// <summary>
/// Ana kategori seed'i her kök kategoriye ayri bir correspondingBusinessType atamalidir;
/// aksi halde istemci isletme tipiyle kategoriyi eslestiremez ve tüm kökler 99 döner.
/// </summary>
public class BusinessCategorySeedTests
{
    [Fact]
    public void RootCategories_ShouldExposeDistinctBusinessTypes()
    {
        var roots = GetRootNodes();

        Assert.Equal(12, roots.Count);

        // Her kök kendi numarasini döndürür: degerler tekrarsiz olmali.
        Assert.Equal(roots.Count, roots.Select(r => r.BusinessType).Distinct().Count());
    }

    [Fact]
    public void OnlyOtherRoot_ShouldReturnOtherBusinessType()
    {
        var roots = GetRootNodes();

        var other = Assert.Single(roots, r => r.BusinessType == EnumBusinessType.Other);

        Assert.Equal("Diğer Hizmetler", other.Name);
    }

    [Fact]
    public void BeautyRoot_ShouldReturnBarberType_SoZeroTypeBusinessesMatch()
    {
        // Kadıköy Berberi gibi businessType = 0 olan isletmelerin kökü Güzellik'tir.
        var beauty = Assert.Single(GetRootNodes(), r => r.Name == "Güzellik ve Kişisel Bakım");

        Assert.Equal(EnumBusinessType.Barber, beauty.BusinessType);
        Assert.Equal(0, (int)beauty.BusinessType!.Value);
    }

    [Fact]
    public void SetCorrespondingBusinessType_ShouldRejectUndefinedValue()
    {
        var category = new BusinessCategory("Test Kategori");

        Assert.Throws<DomainException>(
            () => category.SetCorrespondingBusinessType((EnumBusinessType)1234));
    }

    [Fact]
    public void SetCorrespondingBusinessType_ShouldClearValue_WhenNull()
    {
        var category = new BusinessCategory("Test Kategori", 0, EnumBusinessType.Barber);

        category.SetCorrespondingBusinessType(null);

        Assert.Null(category.CorrespondingBusinessType);
    }

    /// <summary>
    /// Seed'in kök düğümlerini reflection ile okur; böylece üretim kodu
    /// degistirilmeden kategori/ tip eslemesi dogrulanabilir.
    /// </summary>
    private static List<(string Name, EnumBusinessType? BusinessType)> GetRootNodes()
    {
        var method = typeof(BusinessCategorySeed).GetMethod(
            "BuildNodes",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var nodes = (System.Collections.IEnumerable)method!.Invoke(null, null)!;
        var result = new List<(string Name, EnumBusinessType? BusinessType)>();

        foreach (var node in nodes)
        {
            var nodeType = node!.GetType();
            var parentId = (Guid?)nodeType.GetProperty("ParentId")!.GetValue(node);

            // Yalnizca ana kategoriler ilgi alaninda.
            if (parentId is not null)
                continue;

            var name = (string)nodeType.GetProperty("Name")!.GetValue(node)!;
            var businessType = (EnumBusinessType?)nodeType.GetProperty("BusinessType")!.GetValue(node);

            result.Add((name, businessType));
        }

        return result;
    }
}
