using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Kampanya harcaması (manuel giriş veya API senkronu).
    /// İlk sürümde aynı CampaignId+SpendDate+Amount için benzersizlik zorunlu
    /// değildir; manuel tekrar girilebilir.
    /// </summary>
    public class CampaignSpend : BaseEntity
    {
        private static readonly string[] AllowedDataSources = { "manual", "api" };

        public Guid BusinessId { get; private set; }
        public Guid CampaignId { get; private set; }

        /// <summary>Harcama tarihi (işletme lokal saati).</summary>
        public DateTime SpendDate { get; private set; }

        public decimal Amount { get; private set; }
        public string Currency { get; private set; }

        /// <summary>"manual" (kullanıcı girişi) veya "api" (platform senkronu).</summary>
        public string DataSource { get; private set; }

        private CampaignSpend() { }

        public CampaignSpend(
            Guid businessId,
            Guid campaignId,
            DateTime spendDate,
            decimal amount,
            string currency = "TRY",
            string dataSource = "manual")
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (campaignId == Guid.Empty)
                throw new ArgumentException("Kampanya ID boş olamaz.", nameof(campaignId));

            SetAmount(amount);
            SetCurrency(currency);
            SetDataSource(dataSource);

            BusinessId = businessId;
            CampaignId = campaignId;
            SpendDate = spendDate;
        }

        private void SetAmount(decimal amount)
        {
            if (amount < 0)
                throw new DomainException("CampaignSpend.AmountNegative", "Harcama tutarı negatif olamaz.");

            if (amount > 99999999.99m)
                throw new DomainException("CampaignSpend.AmountTooHigh", "Harcama tutarı çok yüksek.");

            Amount = Math.Round(amount, 2);
        }

        private void SetCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new DomainException("CampaignSpend.CurrencyRequired", "Para birimi boş olamaz.");

            if (currency.Length != 3)
                throw new DomainException("CampaignSpend.CurrencyLength", "Para birimi 3 karakter olmalıdır (orn. TRY).");

            Currency = currency.ToUpperInvariant();
        }

        private void SetDataSource(string dataSource)
        {
            var normalized = dataSource?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalized))
                throw new DomainException("CampaignSpend.DataSourceRequired", "Veri kaynağı boş olamaz.");

            if (!AllowedDataSources.Contains(normalized))
                throw new DomainException("CampaignSpend.DataSourceInvalid",
                    "Veri kaynağı yalnızca manual veya api olabilir.");

            DataSource = normalized;
        }
    }
}
