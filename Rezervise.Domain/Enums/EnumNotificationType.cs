namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Bildirim tipleri
    /// </summary>
    public enum EnumNotificationType
    {
        /// <summary>
        /// Randevu Hatirlatma - Randevu öncesi hatirlatma bildirimi
        /// </summary>
        AppointmentReminder = 0,

        /// <summary>
        /// Randevu Onayi - Randevu onaylandi bildirimi
        /// </summary>
        AppointmentConfirmation = 1,

        /// <summary>
        /// Randevu Iptali - Randevu iptal edildi bildirimi
        /// </summary>
        AppointmentCancellation = 2,

        /// <summary>
        /// Randevu Yeniden Planlama - Randevu ertelendi bildirimi
        /// </summary>
        AppointmentReschedule = 3,

        /// <summary>
        /// Promosyon - Kampanya ve indirim bildirimleri
        /// </summary>
        Promotional = 4,

        /// <summary>
        /// Sistem - Sistem kaynakli bildirimler
        /// </summary>
        System = 5,

        /// <summary>
        /// Yorum Istegi - Randevu sonrasi yorum talebi
        /// </summary>
        ReviewRequest = 6,

        /// <summary>
        /// Isletme Dogrulama - Isletme onaylandi bildirimi
        /// </summary>
        BusinessVerification = 7,

        /// <summary>
        /// Randevu Tamamlama - Randevu tamamlandi bildirimi
        /// </summary>
        AppointmentCompleted = 8,

        /// <summary>
        /// Randevu Gelmedi - Müsteri randevuya gelmedi bildirimi
        /// </summary>
        AppointmentNoShow = 9,

        /// <summary>
        /// Ödeme Hatirlatma - Ödeme hatirlatma bildirimi
        /// </summary>
        PaymentReminder = 10,

        /// <summary>
        /// Ödeme Onayi - Ödeme basarili bildirimi
        /// </summary>
        PaymentConfirmation = 11,

        /// <summary>
        /// Ödeme Iptali - Ödeme iptal bildirimi
        /// </summary>
        PaymentCancellation = 12,

        /// <summary>
        /// Yeni Yorum - Isletmeye yeni yorum yapildi bildirimi
        /// </summary>
        NewReview = 13,

        /// <summary>
        /// Favori Hatirlatma - Favori isletmede kampanya bildirimi
        /// </summary>
        FavoriteReminder = 14,

        /// <summary>
        /// Isletme Takip - Isletme takip bildirimi
        /// </summary>
        BusinessFollow = 15,

        /// <summary>
        /// Güvenlik - Güvenlik kaynakli bildirimler
        /// </summary>
        Security = 16,

        /// <summary>
        /// Yeni Mesaj - Yeni mesaj bildirimi
        /// </summary>
        NewMessage = 17,

        /// <summary>
        /// Randevu Iptal Uyarisi - Yakin randevu iptal uyarisi
        /// </summary>
        CancellationWarning = 18,

        /// <summary>
        /// Abonelik - Abonelik kaynakli bildirimler
        /// </summary>
        Subscription = 19,

        /// <summary>
        /// Özel Gün - Dogum günü vb. özel gün bildirimleri
        /// </summary>
        SpecialDay = 20,

        /// <summary>
        /// Acil Durum - Acil durum bildirimleri
        /// </summary>
        Emergency = 21,

        /// <summary>
        /// Rapor - Rapor ve istatistik bildirimleri
        /// </summary>
        Report = 22
    }
}