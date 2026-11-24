using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Rezervise.Infrastructure.Persistence.Converters;

/// <summary>
/// SQL Server time(7) en fazla 23:59:59.9999999 tutabilir; oysa mesai modeli
/// "gün sonu gece yarısı" olan 24:00 değerini destekler (WorkingHour.IsWithinWorkingHours
/// buna göre yazılmıştır). TimeSpan değerler bu yüzden tick (bigint) olarak saklanır;
/// 24:00 dahil tüm değerler kayıpsız gidiş-dönüş yapar.
/// </summary>
public static class TimeSpanTicksConverter
{
    /// <summary>Null olamayan TimeSpan kolonları için tick dönüşümü.</summary>
    public static readonly ValueConverter<TimeSpan, long> Instance = new(
        v => v.Ticks,
        v => new TimeSpan(v));

    /// <summary>Nullable TimeSpan kolonları (mola saatleri) için tick dönüşümü.</summary>
    public static readonly ValueConverter<TimeSpan?, long?> NullableInstance = new(
        v => v.HasValue ? v.Value.Ticks : null,
        v => v.HasValue ? new TimeSpan(v.Value) : null);
}
