using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminCampaigns
{
    /// <summary>
    /// Kampanya özeti: temel alanların yanında toplam harcama (CampaignSpend
    /// toplamı) ve atfedilen randevu sayısı (AppointmentAttribution adedi).
    /// </summary>
    public class AdminCampaignDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public EnumCampaignChannel Channel { get; set; }
        public string? ExternalCampaignId { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Kampanyaya yapılan toplam harcama tutarı.</summary>
        public decimal TotalSpend { get; set; }

        /// <summary>Kampanyaya atfedilen randevu sayısı.</summary>
        public int AttributedAppointments { get; set; }
    }

    /// <summary>Sayfalı kampanya listesi sonucu.</summary>
    public class AdminCampaignListDto
    {
        public IReadOnlyList<AdminCampaignDto> Items { get; set; } = Array.Empty<AdminCampaignDto>();

        /// <summary>Kırpanma sonrası uygulanan sayfa numarası.</summary>
        public int Page { get; set; }

        /// <summary>Kırpanma sonrası uygulanan sayfa boyutu.</summary>
        public int PageSize { get; set; }

        /// <summary>Filtreye uyan toplam kampanya sayısı.</summary>
        public long TotalCount { get; set; }
    }
}
