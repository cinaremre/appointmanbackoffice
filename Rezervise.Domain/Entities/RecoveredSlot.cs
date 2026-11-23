using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// İptal edilen bir randevudan açığa çıkan slot (doküman Modül 10).
    /// İşletme bu slotu bekleme listesinden doldurmaya çalışır; sonuç
    /// Rebooked (dolduruldu) veya Lost (kaybedildi) olur.
    /// </summary>
    public class RecoveredSlot : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid CancelledAppointmentId { get; private set; }

        /// <summary>Slotu dolduran yeni randevu; henüz doldurulmadıysa boş.</summary>
        public Guid? ReplacementAppointmentId { get; private set; }

        public DateTime ReleasedAt { get; private set; }
        public DateTime? RebookedAt { get; private set; }
        public EnumRecoveredSlotStatus Status { get; private set; }

        private RecoveredSlot() { } // EF Core

        public RecoveredSlot(Guid businessId, Guid cancelledAppointmentId, DateTime now)
            : base(Guid.NewGuid())
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (cancelledAppointmentId == Guid.Empty)
                throw new ArgumentException("Randevu ID boş olamaz.", nameof(cancelledAppointmentId));

            BusinessId = businessId;
            CancelledAppointmentId = cancelledAppointmentId;
            ReleasedAt = now;
            Status = EnumRecoveredSlotStatus.Open;
        }

        /// <summary>Slot yeni bir randevuyla dolduruldu.</summary>
        public void Rebook(Guid replacementAppointmentId, DateTime now)
        {
            if (Status != EnumRecoveredSlotStatus.Open)
                throw new DomainException("RecoveredSlot.NotOpen");

            if (replacementAppointmentId == Guid.Empty)
                throw new ArgumentException("Yeni randevu ID boş olamaz.", nameof(replacementAppointmentId));

            ReplacementAppointmentId = replacementAppointmentId;
            RebookedAt = now;
            Status = EnumRecoveredSlotStatus.Rebooked;
            UpdatedAt = now;
        }

        /// <summary>Slot doldurulamadan kaybedildi (süre doldu / başka müşteri aldı).</summary>
        public void MarkLost(DateTime now)
        {
            if (Status != EnumRecoveredSlotStatus.Open)
                throw new DomainException("RecoveredSlot.NotOpen");

            Status = EnumRecoveredSlotStatus.Lost;
            UpdatedAt = now;
        }
    }
}
