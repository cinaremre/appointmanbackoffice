using System.Text.Json;
using Rezervise.Application.Common.Serialization;

namespace Rezervise.Tests.Application;

/// <summary>
/// Tüm API tarih alanları UTC "Z" ekiyle serileştirilmelidir.
/// Gönderilen 2027-01-06T06:00:00Z yanıtta Z'siz ("2027-01-06T06:00:00") geliyordu;
/// converter bunu "2027-01-06T06:00:00Z" yapar. Nullable alanlar da aynı kurala girer.
/// </summary>
public class UtcDateTimeJsonConverterTests
{
    private static JsonSerializerOptions Options()
    {
        // MVC web varsayılanlarıyla aynı: camelCase property adları + converter.
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new UtcDateTimeJsonConverter());
        return options;
    }

    [Fact]
    public void Write_ShouldAppendZSuffix_ForUnspecifiedKindDateTime()
    {
        // EF Core datetime2 değeri Kind=Unspecified döndürür; Z eki zorunlu.
        var value = new DateTime(2027, 1, 6, 6, 0, 0, DateTimeKind.Unspecified);
        var json = JsonSerializer.Serialize(value, Options());
        Assert.Equal("\"2027-01-06T06:00:00Z\"", json);
    }

    [Fact]
    public void Write_ShouldAppendZSuffix_ForUtcKindDateTime()
    {
        var value = new DateTime(2026, 11, 24, 7, 0, 0, DateTimeKind.Utc);
        var json = JsonSerializer.Serialize(value, Options());
        Assert.Equal("\"2026-11-24T07:00:00Z\"", json);
    }

    [Fact]
    public void Write_ShouldSerializeNullableDateTime_WithZSuffix_AndKeepNullAsNull()
    {
        var dto = new DateTimeHolder
        {
            Start = new DateTime(2026, 11, 24, 7, 0, 0, DateTimeKind.Unspecified),
            End = new DateTime(2026, 11, 24, 7, 30, 0, DateTimeKind.Unspecified),
            Optional = null
        };

        var json = JsonSerializer.Serialize(dto, Options());

        Assert.Contains("\"start\":\"2026-11-24T07:00:00Z\"", json);
        Assert.Contains("\"end\":\"2026-11-24T07:30:00Z\"", json);
        Assert.Contains("\"optional\":null", json);
    }

    [Fact]
    public void Read_ShouldParseZSuffixedValue()
    {
        var value = JsonSerializer.Deserialize<DateTime>("\"2027-01-06T06:00:00Z\"", Options());
        Assert.Equal(new DateTime(2027, 1, 6, 6, 0, 0, DateTimeKind.Utc), value);
    }

    private sealed class DateTimeHolder
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public DateTime? Optional { get; set; }
    }
}
