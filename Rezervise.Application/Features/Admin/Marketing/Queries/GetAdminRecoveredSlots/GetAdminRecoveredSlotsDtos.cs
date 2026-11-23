using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminRecoveredSlots
{
    /// <summary>İptal edilen randevudan açığa çıkan slotun yönetici görünümü.</summary>
    public class AdminRecoveredSlotDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }

        /// <summary>İptal edilen randevunun kimliği.</summary>
        public Guid CancelledAppointmentId { get; set; }

        /// <summary>Slotu dolduran yeni randevu; henüz doldurulmadıysa boş.</summary>
        public Guid? ReplacementAppointmentId { get; set; }

        /// <summary>Slotun açığa çıktığı an.</summary>
        public DateTime ReleasedAt { get; set; }

        /// <summary>Slotun yeniden doldurulduğu an; dolmadıysa boş.</summary>
        public DateTime? RebookedAt { get; set; }
        public EnumRecoveredSlotStatus Status { get; set; }
    }

    /// <summary>
    /// Kurtarılan kapasite raporu: slot listesi ve özet toplamlar. Toplam
    /// Rebooked sayısı item DTO'nun dışında, rapor düzeyinde taşınır.
    /// </summary>
    public class AdminRecoveredSlotReportDto
    {
        public IReadOnlyList<AdminRecoveredSlotDto> Items { get; set; }
            = Array.Empty<AdminRecoveredSlotDto>();

        /// <summary>Filtreye uyan slotlar arasındaki toplam Rebooked sayısı.</summary>
        public int TotalRebooked { get; set; }
    }
}
