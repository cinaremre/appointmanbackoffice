namespace Rezervise.Application.Features.Reengagement.Queries.GetReengagementCandidates
{
    /// <summary>
    /// Geri çağırma adayının özeti. SuggestedMessage şablonu değişkenleri
    /// ({{CustomerName}} vb.) bilinçli olarak doldurulmaz; gerçek doldurma
    /// gönderim anında bildirim servisi tarafından yapılır.
    /// </summary>
    public class ReengagementCandidateDto
    {
        /// <summary>Müşterinin User ID'si.</summary>
        public Guid CustomerId { get; set; }

        public Guid ServiceId { get; set; }

        /// <summary>Eşiyen kuralın ID'si; bildirim işaretlemede kullanılır.</summary>
        public Guid RuleId { get; set; }

        public DateTime LastCompletedAt { get; set; }

        public int VisitCount { get; set; }

        /// <summary>Ziyaret sayısı 2'den azsa null.</summary>
        public int? MedianIntervalDays { get; set; }

        /// <summary>Kuralın şablon mesajı; değişkenler doldurulmadan döner.</summary>
        public string SuggestedMessage { get; set; } = string.Empty;

        /// <summary>
        /// İlk sürümde kanal belirlenmediği için müşterinin pazarlama izni
        /// "biliniyor" mü bilgisi döner. Gönderim önce CanContact ile
        /// kanal bazında doğrulanmalıdır.
        /// </summary>
        public bool IsConsentKnown { get; set; }
    }
}
