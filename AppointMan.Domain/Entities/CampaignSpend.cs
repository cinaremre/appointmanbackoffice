using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Kampanya harcamasi (manuel giris veya API senkronu).
    /// Ilk surumde ayni CampaignId+SpendDate+Amount icin benzersizlik zorunlu
    /// degildir; manuel tekrar girilebilir.
    /// </summary>
    public class CampaignSpend : BaseEntity
    {
        private static readonly string[] AllowedDataSources = { "manual", "api" };

        public Guid BusinessId { get; private set; }
        public Guid CampaignId { get; private set; }

        /// <summary>Harcama tarihi (isletme lokal saati).</summary>
        public DateTime SpendDate { get; private set; }

        public decimal Amount { get; private set; }
        public string Currency { get; private set; }

        /// <summary>"manual" (kullanici girisi) veya "api" (platform senkronu).</summary>
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
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (campaignId == Guid.Empty)
                throw new ArgumentException("Kampanya ID bos olamaz.", nameof(campaignId));

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
                throw new DomainException("CampaignSpend.AmountNegative", "Harcama tutari negatif olamaz.");

            if (amount > 99999999.99m)
                throw new DomainException("CampaignSpend.AmountTooHigh", "Harcama tutari cok yuksek.");

            Amount = Math.Round(amount, 2);
        }

        private void SetCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new DomainException("CampaignSpend.CurrencyRequired", "Para birimi bos olamaz.");

            if (currency.Length != 3)
                throw new DomainException("CampaignSpend.CurrencyLength", "Para birimi 3 karakter olmalidir (orn. TRY).");

            Currency = currency.ToUpperInvariant();
        }

        private void SetDataSource(string dataSource)
        {
            var normalized = dataSource?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalized))
                throw new DomainException("CampaignSpend.DataSourceRequired", "Veri kaynagi bos olamaz.");

            if (!AllowedDataSources.Contains(normalized))
                throw new DomainException("CampaignSpend.DataSourceInvalid",
                    "Veri kaynagi yalnizca manual veya api olabilir.");

            DataSource = normalized;
        }
    }
}
