namespace Rezervise.Application.Common.DTOs
{
    /// <summary>
    /// Backoffice listeleme sorgularının sayfalı sonucu (doküman 7.4).
    /// Sayfa değerleri handler tarafında sınırlanır: pageSize en fazla 200'dir.
    /// </summary>
    public class AdminPagedResult<T>
    {
        /// <summary>Sayfadaki kayıtlar.</summary>
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

        /// <summary>1 tabanlı sayfa numarası.</summary>
        public int Page { get; init; }

        /// <summary>Sınırlanmış sayfa boyutu.</summary>
        public int PageSize { get; init; }

        /// <summary>Filtreyle eşleşen toplam kayıt sayısı.</summary>
        public long TotalCount { get; init; }

        /// <summary>Toplam sayfa sayısı.</summary>
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        /// <summary>Sonraki sayfa var mi?</summary>
        public bool HasMore => Page < TotalPages;
    }
}
