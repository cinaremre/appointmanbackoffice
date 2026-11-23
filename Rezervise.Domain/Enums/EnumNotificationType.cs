namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Bildirim tipleri
    /// </summary>
    public enum EnumNotificationType
    {
        /// <summary>
        /// Randevu Hatırlatma - Randevu öncesi hatırlatma bildirimi
        /// </summary>
        AppointmentReminder = 0,

        /// <summary>
        /// Randevu Onayı - Randevu onaylandı bildirimi
        /// </summary>
        AppointmentConfirmation = 1,

        /// <summary>
        /// Randevu İptali - Randevu iptal edildi bildirimi
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
        /// Sistem - Sistem kaynaklı bildirimler
        /// </summary>
        System = 5,

        /// <summary>
        /// Yorum İsteği - Randevu sonrası yorum talebi
        /// </summary>
        ReviewRequest = 6,

        /// <summary>
        /// İşletme Doğrulama - İşletme onaylandı bildirimi
        /// </summary>
        BusinessVerification = 7,

        /// <summary>
        /// Randevu Tamamlama - Randevu tamamlandı bildirimi
        /// </summary>
        AppointmentCompleted = 8,

        /// <summary>
        /// Randevu Gelmedi - Müsteri randevuya gelmedi bildirimi
        /// </summary>
        AppointmentNoShow = 9,

        /// <summary>
        /// Ödeme Hatırlatma - Ödeme hatırlatma bildirimi
        /// </summary>
        PaymentReminder = 10,

        /// <summary>
        /// Ödeme Onayı - Ödeme başarılı bildirimi
        /// </summary>
        PaymentConfirmation = 11,

        /// <summary>
        /// Ödeme İptali - Ödeme iptal bildirimi
        /// </summary>
        PaymentCancellation = 12,

        /// <summary>
        /// Yeni Yorum - İşletmeye yeni yorum yapıldı bildirimi
        /// </summary>
        NewReview = 13,

        /// <summary>
        /// Favori Hatırlatma - Favori işletmede kampanya bildirimi
        /// </summary>
        FavoriteReminder = 14,

        /// <summary>
        /// İşletme Takip - İşletme takip bildirimi
        /// </summary>
        BusinessFollow = 15,

        /// <summary>
        /// Güvenlik - Güvenlik kaynaklı bildirimler
        /// </summary>
        Security = 16,

        /// <summary>
        /// Yeni Mesaj - Yeni mesaj bildirimi
        /// </summary>
        NewMessage = 17,

        /// <summary>
        /// Randevu İptal Uyarısı - Yakın randevu iptal uyarısı
        /// </summary>
        CancellationWarning = 18,

        /// <summary>
        /// Abonelik - Abonelik kaynaklı bildirimler
        /// </summary>
        Subscription = 19,

        /// <summary>
        /// Özel Gün - Doğum günü vb. özel gün bildirimleri
        /// </summary>
        SpecialDay = 20,

        /// <summary>
        /// Açıl Durum - Açıl durum bildirimleri
        /// </summary>
        Emergency = 21,

        /// <summary>
        /// Rapor - Rapor ve istatistik bildirimleri
        /// </summary>
        Report = 22
    }
}