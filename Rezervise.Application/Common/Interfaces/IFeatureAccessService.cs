namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Isletme bazinda ozellik erisim karari (dokuman Modul 14).
    /// Oncelik sirasi (dokuman 14.4):
    /// 1) aktif BusinessFeatureOverride,
    /// 2) aktif abonelik planindaki PlanFeature (IsEnabled + kota),
    /// 3) abonelik yoksa varsayilan temel ozellikler (FeatureCodes.BasicDefaults),
    /// 4) erisim yok.
    /// </summary>
    public interface IFeatureAccessService
    {
        /// <summary>
        /// Isletme icin ozelligin aktif olup olmadigini belirler.
        /// </summary>
        /// <param name="businessId">Isletme ID'si.</param>
        /// <param name="featureCode">FeatureCodes sabitindeki ozellik kodu.</param>
        /// <param name="ct">Iptal belirteci.</param>
        /// <returns>Ozellik erisime aciksa true.</returns>
        Task<bool> IsEnabledAsync(Guid businessId, string featureCode, CancellationToken ct = default);

        /// <summary>
        /// Donemsel kotadan miktar ayirmaya calisir. Kotaya dahil
        /// FeatureUsage kaydini upsert eder; asim varsa false doner
        /// ve sayaci degistirmeden birakir.
        /// </summary>
        /// <param name="businessId">Isletme ID'si.</param>
        /// <param name="featureCode">FeatureCodes sabitindeki ozellik kodu.</param>
        /// <param name="quantity">Ayrilacak miktar.</param>
        /// <param name="ct">Iptal belirteci.</param>
        /// <returns>Rezervasyon basariliysa true; kota asildiysa veya ozellik kapaliysa false.</returns>
        Task<bool> TryReserveQuotaAsync(Guid businessId, string featureCode, int quantity, CancellationToken ct = default);
    }
}
