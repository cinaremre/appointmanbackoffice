namespace Rezervise.Application.Features.Admin
{
    /// <summary>
    /// Backoffice liste sorguları için ortak sayfalama kuralları (doküman 7.4).
    /// Sayfa numarası en az 1; sayfa boyutu 1-200 arasında, istenmezse 50'dir.
    /// Handler'lar gelen değerleri bu kurallara göre kırpar.
    /// </summary>
    public static class AdminPagination
    {
        /// <summary>İstenmediğinde kullanılan sayfa boyutu.</summary>
        public const int DefaultPageSize = 50;

        /// <summary>İzin verilen en büyük sayfa boyutu.</summary>
        public const int MaxPageSize = 200;

        /// <summary>Sayfa numarasını 1'in altına düşmez.</summary>
        /// <param name="page">İstenen sayfa numarası.</param>
        /// <returns>Kırpanmış geçerli sayfa numarası.</returns>
        public static int ClampPage(int page) => page <= 0 ? 1 : page;

        /// <summary>ClampPage ile aynı davranışın eski adı; çağrı yerleri uyumluluk için kullanır.</summary>
        /// <param name="page">İstenen sayfa numarası.</param>
        /// <returns>Kırpanmış geçerli sayfa numarası.</returns>
        public static int NormalizePage(int page) => ClampPage(page);

        /// <summary>
        /// Sayfa boyutunu 1-200 aralığına kırpar; geçersiz veya boş değerlerde
        /// varsayılan 50'yi uygular.
        /// </summary>
        /// <param name="pageSize">İstenen sayfa boyutu.</param>
        /// <returns>Kırpanmış geçerli sayfa boyutu.</returns>
        public static int ClampPageSize(int pageSize)
            => pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
    }
}
