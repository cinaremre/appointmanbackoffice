using Rezervise.Application.Common.Helpers;

namespace Rezervise.Tests.Application;

/// <summary>
/// Mini RFC4180 parser davranislari: tirnakli alan, alan ici virgul/satir,
/// cift tirnak kacisi ve bos satir atlama.
/// </summary>
public class CsvParserTests
{
    [Fact]
    public void Parse_ShouldSplitSimpleRows_ByCommaAndNewline()
    {
        var rows = CsvParser.Parse("Ad,Phone\r\nAli,5321112233\r\nVeli,5322223344");

        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { "Ad", "Phone" }, rows[0]);
        Assert.Equal(new[] { "Ali", "5321112233" }, rows[1]);
        Assert.Equal(new[] { "Veli", "5322223344" }, rows[2]);
    }

    [Fact]
    public void Parse_ShouldSupportQuotedField_ContainingComma()
    {
        var rows = CsvParser.Parse("\"Ad, Soyad\",X");

        Assert.Single(rows);
        Assert.Equal(2, rows[0].Length);
        Assert.Equal("Ad, Soyad", rows[0][0]);
        Assert.Equal("X", rows[0][1]);
    }

    [Fact]
    public void Parse_ShouldSupportQuotedField_ContainingNewline()
    {
        var rows = CsvParser.Parse("\"satir1\nsatir2\",X");

        Assert.Single(rows);
        Assert.Equal("satir1\nsatir2", rows[0][0]);
        Assert.Equal("X", rows[0][1]);
    }

    [Fact]
    public void Parse_ShouldUnescapeDoubledQuotes()
    {
        var rows = CsvParser.Parse("\"Mustafa \"\"Ozkan\"\" Yilmaz\",5321112233");

        Assert.Single(rows);
        Assert.Equal("Mustafa \"Ozkan\" Yilmaz", rows[0][0]);
    }

    [Fact]
    public void Parse_ShouldSkipEmptyLines()
    {
        var rows = CsvParser.Parse("a,b\n\nc,d\n\n\ne,f");

        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { "a", "b" }, rows[0]);
        Assert.Equal(new[] { "c", "d" }, rows[1]);
        Assert.Equal(new[] { "e", "f" }, rows[2]);
    }

    [Fact]
    public void Parse_ShouldTreatTrailingComma_AsEmptyField()
    {
        var rows = CsvParser.Parse("a,b,");

        Assert.Single(rows);
        Assert.Equal(3, rows[0].Length);
        Assert.Equal(string.Empty, rows[0][2]);
    }

    [Fact]
    public void Parse_ShouldReturnNoRows_ForNullOrWhitespaceContent()
    {
        Assert.Empty(CsvParser.Parse(string.Empty));
        Assert.Empty(CsvParser.Parse("   "));
        Assert.Empty(CsvParser.Parse(null!));
    }

    [Fact]
    public void Parse_ShouldHandleCRLFAndLF_Mixed()
    {
        var rows = CsvParser.Parse("a,b\r\nc,d\ne,f");

        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { "e", "f" }, rows[2]);
    }
}
