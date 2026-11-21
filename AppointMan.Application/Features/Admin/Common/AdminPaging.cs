namespace AppointMan.Application.Features.Admin.Common
{
    /// <summary>
    /// Backoffice liste ve toplu islem sorgulari icin ortak sinirlar
    /// (dokuman 7.4 ve Modul 10). Sayfa numarasi en az 1; sayfa boyutu
    /// 1-200 arasinda, istenmezse 50'dir. Toplu islemler ilk surumde
    /// en fazla 2000 kayit kabul eder; buyuk olcek kuyruk tabanli
    /// islemciye tasinacaktir.
    /// </summary>
    public static class AdminPaging
    {
        /// <summary>Istenmediginde kullanilan sayfa boyutu.</summary>
        public const int DefaultPageSize = 50;

        /// <summary>Izin verilen en buyuk sayfa boyutu.</summary>
        public const int MaxPageSize = 200;

        /// <summary>Toplu islemde tek istekte islenebilecek en fazla kayit sayisi.</summary>
        public const int MaxBulkRecords = 2000;

        /// <summary>Sayfa numarasini 1'in altina dusmez.</summary>
        /// <param name="page">Istenen sayfa numarasi.</param>
        /// <returns>Kirpanmis gecerli sayfa numarasi.</returns>
        public static int NormalizePage(int page) => page <= 0 ? 1 : page;

        /// <summary>
        /// Sayfa boyutunu 1-200 araligina kirpar; gecersiz veya bos
        /// degerlerde varsayilan 50'yi uygular.
        /// </summary>
        /// <param name="pageSize">Istenen sayfa boyutu.</param>
        /// <returns>Kirpanmis gecerli sayfa boyutu.</returns>
        public static int ClampPageSize(int pageSize)
            => pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
    }
}
