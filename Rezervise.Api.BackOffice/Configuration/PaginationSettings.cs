namespace Rezervise.BackOffice.Api.Configuration
{
    /// <summary>
    /// Backoffice listeleme sınırları (doküman 7.4).
    /// </summary>
    public sealed class PaginationSettings
    {
        public const string SectionName = "BackOfficePagination";

        public int DefaultPageSize { get; init; } = 50;

        /// <summary>İzin verilen en büyük sayfa boyutu.</summary>
        public int MaxPageSize { get; init; } = 200;

        /// <summary>Verilen sayfa boyutunu sınırlar; en az 1.</summary>
        public int ClampPageSize(int requested)
            => Math.Clamp(requested <= 0 ? DefaultPageSize : requested, 1, MaxPageSize);
    }
}
