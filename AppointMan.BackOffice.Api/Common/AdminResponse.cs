namespace AppointMan.BackOffice.Api.Common
{
    /// <summary>
    /// Backoffice response envelope'u (dokuman 7.2).
    /// Tekil cevap: data + meta. Liste cevap: data + pagination + meta.
    /// </summary>
    public sealed class AdminResponse<T>
    {
        public T? Data { get; init; }

        public PaginationMeta? Pagination { get; init; }

        public AdminMeta Meta { get; init; } = new();
    }

    /// <summary>Liste cevabinin sayfalama bilgisi.</summary>
    public sealed class PaginationMeta
    {
        public int Page { get; init; }

        public int PageSize { get; init; }

        public long TotalCount { get; init; }

        public int TotalPages { get; init; }

        public bool HasMore { get; init; }
    }

    /// <summary>Request korelasyon bilgisi.</summary>
    public sealed class AdminMeta
    {
        public string CorrelationId { get; init; } = string.Empty;
    }
}
