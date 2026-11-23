namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Randevu durumları
    /// </summary>
    public enum EnumAppointmentStatus
    {
        /// <summary>
        /// Beklemede - Randevu oluşturuldu, henüz onaylanmadı
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Onaylandı - Randevu onaylandı, gerçeklesmeyi bekliyor
        /// </summary>
        Confirmed = 1,

        /// <summary>
        /// Tamamlandı - Randevu başarıyla gerçeklesti
        /// </summary>
        Completed = 2,

        /// <summary>
        /// İptal edildi - Randevu iptal edildi
        /// </summary>
        Cancelled = 3,

        /// <summary>
        /// Gelmedi - Müsteri randevuya gelmedi (No-Show)
        /// </summary>
        NoShow = 4,

        /// <summary>
        /// Yeniden planlandı - Randevu başka bir tarihe ertelendi
        /// </summary>
        Rescheduled = 5
    }
}