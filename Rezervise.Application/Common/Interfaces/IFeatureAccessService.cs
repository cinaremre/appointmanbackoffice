namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// İşletme bazında özellik erişim kararı (doküman Modül 14).
    /// Öncelik sırası (doküman 14.4):
    /// 1) aktif BusinessFeatureOverride,
    /// 2) aktif abonelik planındaki PlanFeature (IsEnabled + kota),
    /// 3) abonelik yoksa varsayılan temel özellikler (FeatureCodes.BasicDefaults),
    /// 4) erişim yok.
    /// </summary>
    public interface IFeatureAccessService
    {
        /// <summary>
        /// İşletme için özelliğin aktif olup olmadığını belirler.
        /// </summary>
        /// <param name="businessId">İşletme ID'si.</param>
        /// <param name="featureCode">FeatureCodes sabitindeki özellik kodu.</param>
        /// <param name="ct">İptal belirteci.</param>
        /// <returns>Özellik erişime açıksa true.</returns>
        Task<bool> IsEnabledAsync(Guid businessId, string featureCode, CancellationToken ct = default);

        /// <summary>
        /// Dönemsel kotadan miktar ayırmaya çalışır. Kotaya dahil
        /// FeatureUsage kaydını upsert eder; asım varsa false döner
        /// ve sayacı değiştirmeden bırakır.
        /// </summary>
        /// <param name="businessId">İşletme ID'si.</param>
        /// <param name="featureCode">FeatureCodes sabitindeki özellik kodu.</param>
        /// <param name="quantity">Ayrılacak miktar.</param>
        /// <param name="ct">İptal belirteci.</param>
        /// <returns>Rezervasyon başarılıysa true; kota asıldıysa veya özellik kapalıysa false.</returns>
        Task<bool> TryReserveQuotaAsync(Guid businessId, string featureCode, int quantity, CancellationToken ct = default);
    }
}
