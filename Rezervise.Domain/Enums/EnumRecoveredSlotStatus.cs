namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// İptal edilen randevudan açığa çıkan slotun durumu (doküman Modül 10).
    /// </summary>
    public enum EnumRecoveredSlotStatus
    {
        /// <summary>Slot açık; bekleme listesinden doldurulabilir.</summary>
        Open = 0,

        /// <summary>Slot bekleme listesinden gelen müşteriyle yeniden dolduruldu.</summary>
        Rebooked = 1,

        /// <summary>Slot doldurulamadan kaybedildi (süre doldu / başka müşteri aldı).</summary>
        Lost = 2
    }
}
