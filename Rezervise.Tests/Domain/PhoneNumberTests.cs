using Rezervise.Domain.Exceptions;
using Rezervise.Domain.ValueObjects;

namespace Rezervise.Tests.Domain;

public class PhoneNumberTests
{
    /// <summary>
    /// Ayni numaranin farkli yazimlari TEK bir kanonik deger uretmelidir; kayit
    /// akisindaki tekillik kontrolu bu degere dayanir.
    /// </summary>
    [Theory]
    [InlineData("05551112233")]
    [InlineData("5551112233")]
    [InlineData("+905551112233")]
    [InlineData("+90 555 111 22 33")]
    [InlineData("+90 (0555) 111 22 33")]
    [InlineData("0090 555 111 22 33")]
    [InlineData("905551112233")]
    public void Value_ShouldBeCanonical_ForEveryTrFormat(string raw)
    {
        var phone = new PhoneNumber(raw);

        Assert.Equal("+905551112233", phone.Value);
        Assert.Equal("+90", phone.CountryCode);
        Assert.Equal("5551112233", phone.NationalNumber);
    }

    [Fact]
    public void Value_ShouldSplitNonTrCountryCode_NotTreatItAsNational()
    {
        var phone = new PhoneNumber("+1 (555) 123-4567");

        Assert.Equal("+15551234567", phone.Value);
        Assert.Equal("+1", phone.CountryCode);
        Assert.Equal("5551234567", phone.NationalNumber);
    }

    [Fact]
    public void Equality_ShouldCompareCanonicalValue()
    {
        PhoneNumber fromNational = "05551112233";
        var fromInternational = new PhoneNumber("+905551112233");

        Assert.True(fromNational == fromInternational);
        Assert.Equal(fromNational.GetHashCode(), fromInternational.GetHashCode());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("telefon")]
    public void Constructor_ShouldThrow_ForInvalidInput(string raw)
    {
        Assert.Throws<DomainException>(() => new PhoneNumber(raw));
    }
}
