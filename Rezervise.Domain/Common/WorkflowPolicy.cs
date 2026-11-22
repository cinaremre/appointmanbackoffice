namespace Rezervise.Domain.Common
{
    /// <summary>
    /// Randevu iş akışının zaman-based kuralları. Tek kaynak olacak şekilde
    /// domain katmanında tutulur; handler'lar ve arka plan servisleri aynı
    /// değerleri kullanır.
    /// </summary>
    public static class WorkflowPolicy
    {
        /// <summary>Randevudan kaç saat önce hatırlatma gönderileceği.</summary>
        public static readonly TimeSpan ReminderLeadTime = TimeSpan.FromHours(24);

        /// <summary>İptal ve yeniden planlama için randevu başlangıcına kalan asgari süre (saat).</summary>
        public const int CustomerChangeCutoffHours = 2;

        /// <summary>İptal ve yeniden planlama için randevu başlangıcına kalan asgari süre.</summary>
        public static readonly TimeSpan MinimumActionLeadTime = TimeSpan.FromHours(CustomerChangeCutoffHours);

        /// <summary>Bitiş saatinden sonra randevunun otomatik tamamlanacağı süre.</summary>
        public static readonly TimeSpan AutoCompleteGracePeriod = TimeSpan.FromHours(1);

        /// <summary>Arka plan tarayıcısının çalışma periyodu.</summary>
        public static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);
    }
}
