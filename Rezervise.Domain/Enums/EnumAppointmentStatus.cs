namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Randevu durumlari
    /// </summary>
    public enum EnumAppointmentStatus
    {
        /// <summary>
        /// Beklemede - Randevu olusturuldu, henüz onaylanmadi
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Onaylandi - Randevu onaylandi, gerçeklesmeyi bekliyor
        /// </summary>
        Confirmed = 1,

        /// <summary>
        /// Tamamlandi - Randevu basariyla gerçeklesti
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Iptal edildi - Randevu iptal edildi
        /// </summary>
        Cancelled = 3,

        /// <summary>
        /// Gelmedi - Müsteri randevuya gelmedi (No-Show)
        /// </summary>
        NoShow = 4,

        /// <summary>
        /// Yeniden planlandi - Randevu baska bir tarihe ertelendi
        /// </summary>
        Rescheduled = 5
    }
}