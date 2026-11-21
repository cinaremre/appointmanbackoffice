namespace AppointMan.Application.Features.Admin
{
    /// <summary>
    /// Backoffice liste sorgulari icin ortak sayfalama kurallari (dokuman 7.4).
    /// Sayfa numarasi en az 1; sayfa boyutu 1-200 arasinda, istenmezse 50'dir.
    /// Handler'lar gelen degerleri bu kurallara gore kirpar.
    /// </summary>
    public static class AdminPagination
    {
        /// <summary>Istenmediginde kullanilan sayfa boyutu.</summary>
        public const int DefaultPageSize = 50;

        /// <summary>Izin verilen en buyuk sayfa boyutu.</summary>
        public const int MaxPageSize = 200;

        /// <summary>Sayfa numarasini 1'in altina dusmez.</summary>
        /// <param name="page">Istenen sayfa numarasi.</param>
        /// <returns>Kirpanmis gecerli sayfa numarasi.</returns>
        public static int ClampPage(int page) => page <= 0 ? 1 : page;

        /// <summary>ClampPage ile ayni davranisin eski adi; cagri yerleri uyumluluk icin kullanir.</summary>
        /// <param name="page">Istenen sayfa numarasi.</param>
        /// <returns>Kirpanmis gecerli sayfa numarasi.</returns>
        public static int NormalizePage(int page) => ClampPage(page);

        /// <summary>
        /// Sayfa boyutunu 1-200 araligina kirpar; gecersiz veya bos degerlerde
        /// varsayilan 50'yi uygular.
        /// </summary>
        /// <param name="pageSize">Istenen sayfa boyutu.</param>
        /// <returns>Kirpanmis gecerli sayfa boyutu.</returns>
        public static int ClampPageSize(int pageSize)
            => pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
    }
}
