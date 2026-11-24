using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rezervise.Application.Common.Serialization;

/// <summary>
/// API yanıtlarındaki DateTime değerlerini UTC olarak işaretleyip ISO-8601 "Z" ekiyle yazar.
/// EF Core datetime2 kolonlarını Kind=Unspecified döndürür; System.Text.Json bu değerleri
/// saat dilimi eki olmadan ("2027-01-06T06:00:00") serileştirir. Suffix'siz değerler mobil
/// tarafta UTC varsayılıp yanlış yerel saate çevrilebildiği için tüm tarih alanları tek
/// kuraldan geçer: saklanan değer UTC'dir, yanıt "2027-01-06T06:00:00Z" biçiminde döner.
/// Nullable DateTime alanları için de aynı converter geçerlidir (STJ otomatik sarar).
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    /// <summary>Z-suffixed, offsetsiz veya yerel değerleri olduğu gibi okur (varsayılan davranış).</summary>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime();

    /// <summary>Değeri UTC olarak işaretleyip "Z" ekiyle yazar (Kind fark etmeksizin).</summary>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
