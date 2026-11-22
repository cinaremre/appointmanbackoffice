using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminRecoveredSlots
{
    /// <summary>Iptal edilen randevudan aciga cikan slotun yonetici gorunumu.</summary>
    public class AdminRecoveredSlotDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }

        /// <summary>Iptal edilen randevunun kimligi.</summary>
        public Guid CancelledAppointmentId { get; set; }

        /// <summary>Slotu dolduran yeni randevu; henuz doldurulmadiysa bos.</summary>
        public Guid? ReplacementAppointmentId { get; set; }

        /// <summary>Slotun aciga ciktigi an.</summary>
        public DateTime ReleasedAt { get; set; }

        /// <summary>Slotun yeniden dolduruldugu an; dolmadiysa bos.</summary>
        public DateTime? RebookedAt { get; set; }
        public EnumRecoveredSlotStatus Status { get; set; }
    }

    /// <summary>
    /// Kurtarilan kapasite raporu: slot listesi ve ozet toplamlar. Toplam
    /// Rebooked sayisi item DTO'nun disinda, rapor duzeyinde tasinir.
    /// </summary>
    public class AdminRecoveredSlotReportDto
    {
        public IReadOnlyList<AdminRecoveredSlotDto> Items { get; set; }
            = Array.Empty<AdminRecoveredSlotDto>();

        /// <summary>Filtreye uyan slotlar arasindaki toplam Rebooked sayisi.</summary>
        public int TotalRebooked { get; set; }
    }
}
