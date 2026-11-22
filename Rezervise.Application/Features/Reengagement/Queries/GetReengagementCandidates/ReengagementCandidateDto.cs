namespace Rezervise.Application.Features.Reengagement.Queries.GetReengagementCandidates
{
    /// <summary>
    /// Geri cagirma adayinin ozeti. SuggestedMessage sablonu degiskenleri
    /// ({{CustomerName}} vb.) bilincli olarak doldurulmaz; gercek doldurma
    /// gonderim aninda bildirim servisi tarafindan yapilir.
    /// </summary>
    public class ReengagementCandidateDto
    {
        /// <summary>Musterinin User ID'si.</summary>
        public Guid CustomerId { get; set; }

        public Guid ServiceId { get; set; }

        /// <summary>Esiyen kuralin ID'si; bildirim isaretlemede kullanilir.</summary>
        public Guid RuleId { get; set; }

        public DateTime LastCompletedAt { get; set; }

        public int VisitCount { get; set; }

        /// <summary>Ziyaret sayisi 2'den azsa null.</summary>
        public int? MedianIntervalDays { get; set; }

        /// <summary>Kuralin sablon mesaji; degiskenler doldurulmadan doner.</summary>
        public string SuggestedMessage { get; set; } = string.Empty;

        /// <summary>
        /// Ilk surumde kanal belirlenmedigi icin musterinin pazarlama izni
        /// "biliniyor" mu bilgisi doner. Gonderim once CanContact ile
        /// kanal bazinda dogrulanmalidir.
        /// </summary>
        public bool IsConsentKnown { get; set; }
    }
}
