using Rezervise.Application.Common.Interfaces;
using Rezervise.Domain.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;

namespace Rezervise.Infrastructure.Services
{
    /// <summary>
    /// Ozellik erisim kararinin tek kaynagi (dokuman Modul 14.4).
    ///
    /// Oncelik sirasi:
    /// 1. Aktif BusinessFeatureOverride - IsEnabled ve QuotaOverride plani ezer.
    ///    Gecerlilik araligi [ValidFrom, ValidUntil); suresi dolmus override yok sayilir.
    /// 2. Isletmenin aktif BusinessSubscription'inin planindaki PlanFeature -
    ///    IsEnabled ve IncludedQuota esas alinir.
    /// 3. Abonelik yoksa varsayilan temel ozellikler (FeatureCodes.BasicDefaults)
    ///    acik kabul edilir; diger tum ozellikler kapalidir.
    /// 4. Tanimli olmayan ozellik kodu = erisim yok.
    ///
    /// Kota: IncludedQuota/QuotaOverride null ise sinirsizdir; override kotasi
    /// null ise plan kotasi gecerli kalir. TryReserveQuota donemlik FeatureUsage
    /// kaydini upsert eder; asim varsa sayac degistirmeden false doner.
    /// </summary>
    public class FeatureAccessService : IFeatureAccessService
    {
        private readonly IRepository<BusinessFeatureOverride> _overrideRepository;
        private readonly IRepository<BusinessSubscription> _subscriptionRepository;
        private readonly IRepository<PlanFeature> _planFeatureRepository;
        private readonly IRepository<FeatureDefinition> _featureDefinitionRepository;
        private readonly IRepository<FeatureUsage> _usageRepository;
        private readonly IDateTimeService _dateTime;

        public FeatureAccessService(
            IRepository<BusinessFeatureOverride> overrideRepository,
            IRepository<BusinessSubscription> subscriptionRepository,
            IRepository<PlanFeature> planFeatureRepository,
            IRepository<FeatureDefinition> featureDefinitionRepository,
            IRepository<FeatureUsage> usageRepository,
            IDateTimeService dateTime)
        {
            _overrideRepository = overrideRepository;
            _subscriptionRepository = subscriptionRepository;
            _planFeatureRepository = planFeatureRepository;
            _featureDefinitionRepository = featureDefinitionRepository;
            _usageRepository = usageRepository;
            _dateTime = dateTime;
        }

        public async Task<bool> IsEnabledAsync(Guid businessId, string featureCode, CancellationToken ct = default)
        {
            var entitlement = await ResolveAsync(businessId, featureCode, ct);
            return entitlement.IsEnabled;
        }

        public async Task<bool> TryReserveQuotaAsync(Guid businessId, string featureCode, int quantity, CancellationToken ct = default)
        {
            // 1. Ozelligin etkin durumu ve kotasi cozulur.
            var entitlement = await ResolveAsync(businessId, featureCode, ct);

            if (!entitlement.IsEnabled)
                return false;

            if (quantity < 0)
                return false;

            // 2. Donemlik kullanim kaydini bul ("YYYY-MM").
            var now = _dateTime.UtcNow;
            var period = $"{now.Year}-{now.Month:00}";

            var usage = (await _usageRepository.FindAsync(
                u => u.BusinessId == businessId
                    && u.FeatureId == entitlement.FeatureId
                    && u.UsagePeriod == period
                    && !u.IsDeleted,
                ct)).FirstOrDefault();

            var used = usage?.UsedQuantity ?? 0;

            // 3. Kota kontrolu: null kota = sinirsiz.
            if (entitlement.Quota.HasValue && used + quantity > entitlement.Quota.Value)
                return false;

            // 4. Upsert: kayit yoksa olustur, varsa mutlak yeni toplami yaz.
            if (usage is null)
            {
                var newUsage = new FeatureUsage(businessId, entitlement.FeatureId, period, quantity);
                await _usageRepository.AddAsync(newUsage, ct);
            }
            else
            {
                usage.SetUsedQuantity(used + quantity);
                await _usageRepository.UpdateAsync(usage, ct);
            }

            return true;
        }

        /// <summary>Erisim karari ve gecerli kotanin cozulmus hali.</summary>
        private sealed record FeatureEntitlement(Guid FeatureId, bool IsEnabled, int? Quota)
        {
            // default(record) null referans uretir; None acikca tanimlanir.
            public static FeatureEntitlement None => new(Guid.Empty, false, null);
        }

        private async Task<FeatureEntitlement> ResolveAsync(Guid businessId, string featureCode, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(featureCode))
                return FeatureEntitlement.None;

            // Tanimsiz ozellik kodu = erisim yok.
            var definition = (await _featureDefinitionRepository.FindAsync(
                d => d.Code == featureCode && !d.IsDeleted,
                ct)).FirstOrDefault();

            if (definition is null || !definition.IsActive)
                return FeatureEntitlement.None;

            var now = _dateTime.UtcNow;

            // 1. Aktif override: gecerlilik araligindaki en yeni kayit kazanir.
            var overrides = await _overrideRepository.FindAsync(
                o => o.BusinessId == businessId && o.FeatureId == definition.Id && !o.IsDeleted,
                ct);

            var activeOverride = overrides
                .Where(o => o.IsActiveAt(now))
                .OrderByDescending(o => o.ValidFrom)
                .FirstOrDefault();

            if (activeOverride is not null)
                return new FeatureEntitlement(definition.Id, activeOverride.IsEnabled, activeOverride.QuotaOverride);

            // 2. Aktif abonelik planindaki ozellik satiri.
            var subscriptions = await _subscriptionRepository.FindAsync(
                s => s.BusinessId == businessId && !s.IsDeleted,
                ct);

            var activeSubscription = subscriptions.FirstOrDefault(s => s.IsActiveAt(now));

            if (activeSubscription is not null)
            {
                var planFeature = (await _planFeatureRepository.FindAsync(
                    p => p.PlanId == activeSubscription.PlanId
                        && p.FeatureId == definition.Id
                        && !p.IsDeleted,
                    ct)).FirstOrDefault();

                return new FeatureEntitlement(definition.Id, planFeature?.IsEnabled ?? false, planFeature?.IncludedQuota);
            }

            // 3. Abonelik yok: temel ozellikler acik, digerleri kapali.
            if (FeatureCodes.BasicDefaults.Contains(featureCode))
                return new FeatureEntitlement(definition.Id, true, null);

            // 4. Erisim yok.
            return FeatureEntitlement.None;
        }
    }
}
