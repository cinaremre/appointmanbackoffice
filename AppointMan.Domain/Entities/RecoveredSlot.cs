using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Iptal edilen bir randevudan aciga cikan slot (dokuman Modul 10).
    /// Isletme bu slotu bekleme listesinden doldurmaya calisir; sonuc
    /// Rebooked (dolduruldu) veya Lost (kaybedildi) olur.
    /// </summary>
    public class RecoveredSlot : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid CancelledAppointmentId { get; private set; }

        /// <summary>Slotu dolduran yeni randevu; henuz doldurulmadiysa bos.</summary>
        public Guid? ReplacementAppointmentId { get; private set; }

        public DateTime ReleasedAt { get; private set; }
        public DateTime? RebookedAt { get; private set; }
        public EnumRecoveredSlotStatus Status { get; private set; }

        private RecoveredSlot() { } // EF Core

        public RecoveredSlot(Guid businessId, Guid cancelledAppointmentId, DateTime now)
            : base(Guid.NewGuid())
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (cancelledAppointmentId == Guid.Empty)
                throw new ArgumentException("Randevu ID bos olamaz.", nameof(cancelledAppointmentId));

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
                throw new ArgumentException("Yeni randevu ID bos olamaz.", nameof(replacementAppointmentId));

            ReplacementAppointmentId = replacementAppointmentId;
            RebookedAt = now;
            Status = EnumRecoveredSlotStatus.Rebooked;
            UpdatedAt = now;
        }

        /// <summary>Slot doldurulamadan kaybedildi (sure doldu / baska musteri aldi).</summary>
        public void MarkLost(DateTime now)
        {
            if (Status != EnumRecoveredSlotStatus.Open)
                throw new DomainException("RecoveredSlot.NotOpen");

            Status = EnumRecoveredSlotStatus.Lost;
            UpdatedAt = now;
        }
    }
}
