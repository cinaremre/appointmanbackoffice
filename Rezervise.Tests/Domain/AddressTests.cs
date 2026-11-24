using Rezervise.Domain.Exceptions;
using Rezervise.Domain.ValueObjects;

namespace Rezervise.Tests.Domain;

/// <summary>
/// Posta kodu opsiyonel olmalı: null/boş postal code ile adres kurulabilmeli
/// (önceden "Posta kodu boş olamaz." hatası veriyordu) ve tek metin adres
/// posta kodu olmadan düzgün oluşmalı.
/// </summary>
public class AddressTests
{
    [Fact]
    public void Constructor_ShouldAllowNullPostalCode()
    {
        var address = new Address(34, 438, "Moda Caddesi No: 45", null);

        Assert.Null(address.PostalCode);
        Assert.Equal(34, address.CityId);
        Assert.Equal(438, address.DistrictId);
    }

    [Fact]
    public void Constructor_ShouldTreatWhitespacePostalCodeAsNull()
    {
        var address = new Address(34, null, "Moda Caddesi No: 45", "   ");

        Assert.Null(address.PostalCode);
    }

    [Fact]
    public void Constructor_ShouldKeepTrimmedPostalCode()
    {
        var address = new Address(34, null, "Moda Caddesi No: 45", " 34710 ");

        Assert.Equal("34710", address.PostalCode);
    }

    [Fact]
    public void SetPostalCode_ShouldAllowClearingWithNull()
    {
        var address = new Address(34, null, "Moda Caddesi No: 45", "34710");

        address.SetPostalCode(null);

        Assert.Null(address.PostalCode);
    }

    [Fact]
    public void SetPostalCode_ShouldStillRejectTooLongValue()
    {
        var address = new Address(34, null, "Moda Caddesi No: 45", "34710");

        Assert.Throws<DomainException>(() => address.SetPostalCode(new string('1', 21)));
    }

    [Fact]
    public void GetFullAddress_ShouldSkipMissingPostalCode()
    {
        var address = new Address(34, null, "Moda Caddesi No: 45", null);

        var full = address.GetFullAddress();

        Assert.Equal("Moda Caddesi No: 45", full);
        Assert.DoesNotContain("null", full);
    }
}
