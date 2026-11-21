using AppointMan.Domain.Enums;

namespace AppointMan.Application.Features.Admin.Marketing.Queries.GetAdminCampaigns
{
    /// <summary>
    /// Kampanya ozeti: temel alanlarin yaninda toplam harcama (CampaignSpend
    /// toplami) ve atfedilen randevu sayisi (AppointmentAttribution adedi).
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

        /// <summary>Kampanyaya yapilan toplam harcama tutari.</summary>
        public decimal TotalSpend { get; set; }

        /// <summary>Kampanyaya atfedilen randevu sayisi.</summary>
        public int AttributedAppointments { get; set; }
    }

    /// <summary>Sayfali kampanya listesi sonucu.</summary>
    public class AdminCampaignListDto
    {
        public IReadOnlyList<AdminCampaignDto> Items { get; set; } = Array.Empty<AdminCampaignDto>();

        /// <summary>Kirpanma sonrasi uygulanan sayfa numarasi.</summary>
        public int Page { get; set; }

        /// <summary>Kirpanma sonrasi uygulanan sayfa boyutu.</summary>
        public int PageSize { get; set; }

        /// <summary>Filtreye uyan toplam kampanya sayisi.</summary>
        public long TotalCount { get; set; }
    }
}
