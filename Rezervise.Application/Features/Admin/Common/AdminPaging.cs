namespace Rezervise.Application.Features.Admin.Common
{
    /// <summary>
    /// Backoffice liste ve toplu işlem sorguları için ortak sınırlar
    /// (doküman 7.4 ve Modül 10). Sayfa numarası en az 1; sayfa boyutu
    /// 1-200 arasında, istenmezse 50'dir. Toplu işlemler ilk sürümde
    /// en fazla 2000 kayıt kabul eder; büyük ölçek kuyruk tabanlı
    /// işlemciye taşınacaktır.
    /// </summary>
    public static class AdminPaging
    {
        /// <summary>İstenmediğinde kullanılan sayfa boyutu.</summary>
        public const int DefaultPageSize = 50;

        /// <summary>İzin verilen en büyük sayfa boyutu.</summary>
        public const int MaxPageSize = 200;

        /// <summary>Toplu işlemde tek istekte işlenebilecek en fazla kayıt sayısı.</summary>
        public const int MaxBulkRecords = 2000;

        /// <summary>Sayfa numarasını 1'in altına düşmez.</summary>
        /// <param name="page">İstenen sayfa numarası.</param>
        /// <returns>Kırpanmış geçerli sayfa numarası.</returns>
        public static int NormalizePage(int page) => page <= 0 ? 1 : page;

        /// <summary>
        /// Sayfa boyutunu 1-200 aralığına kırpar; geçersiz veya boş
        /// değerlerde varsayılan 50'yi uygular.
        /// </summary>
        /// <param name="pageSize">İstenen sayfa boyutu.</param>
        /// <returns>Kırpanmış geçerli sayfa boyutu.</returns>
        public static int ClampPageSize(int pageSize)
            => pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
    }
}
