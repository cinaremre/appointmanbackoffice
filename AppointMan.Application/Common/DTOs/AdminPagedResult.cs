namespace AppointMan.Application.Common.DTOs
{
    /// <summary>
    /// Backoffice listeleme sorgularinin sayfali sonucu (dokuman 7.4).
    /// Sayfa degerleri handler tarafinda sinirlanir: pageSize en fazla 200'dir.
    /// </summary>
    public class AdminPagedResult<T>
    {
        /// <summary>Sayfadaki kayitlar.</summary>
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; init; }

        /// <summary>Sinirlanmis sayfa boyutu.</summary>
        public int PageSize { get; init; }

        /// <summary>Filtreyle eslesen toplam kayit sayisi.</summary>
        public long TotalCount { get; init; }

        /// <summary>Toplam sayfa sayisi.</summary>
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        /// <summary>Sonraki sayfa var mi?</summary>
        public bool HasMore => Page < TotalPages;
    }
}
