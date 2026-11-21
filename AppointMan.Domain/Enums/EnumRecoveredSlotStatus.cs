namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Iptal edilen randevudan aciga cikan slotun durumu (dokuman Modul 10).
    /// </summary>
    public enum EnumRecoveredSlotStatus
    {
        /// <summary>Slot acik; bekleme listesinden doldurulabilir.</summary>
        Open = 0,

        /// <summary>Slot bekleme listesinden gelen musteriyle yeniden dolduruldu.</summary>
        Rebooked = 1,

        /// <summary>Slot doldurulamadan kaybedildi (sure doldu / baska musteri aldi).</summary>
        Lost = 2
    }
}
