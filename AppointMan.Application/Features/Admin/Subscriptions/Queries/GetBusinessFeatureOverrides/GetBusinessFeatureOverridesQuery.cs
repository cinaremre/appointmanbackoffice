using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Subscriptions.Queries.GetBusinessFeatureOverrides
{
    /// <summary>
    /// Isletmeye verilmis ozellik override kararlari listeler
    /// (dokuman Modul 13). Gecmis kayitlar da gorunur; yonetici arayuzu
    /// gecerlilik araligindan aktif olanlari secer.
    /// </summary>
    public class GetBusinessFeatureOverridesQuery : IRequest<Result<IReadOnlyList<AdminFeatureOverrideDto>>>
    {
        public Guid BusinessId { get; set; }
    }

    /// <summary>Tek override karanin admin gorunumu.</summary>
    public class AdminFeatureOverrideDto
    {
        public Guid Id { get; set; }
        public Guid FeatureId { get; set; }
        public string FeatureCode { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }

        /// <summary>Null ise plan kotasi gecerli kalir.</summary>
        public int? QuotaOverride { get; set; }

        public DateTime ValidFrom { get; set; }

        /// <summary>UTC gecerlilik sonu; null = belirsiz sure.</summary>
        public DateTime? ValidUntil { get; set; }

        public string? Reason { get; set; }

        /// <summary>Karari veren platform yoneticisi.</summary>
        public Guid GrantedBy { get; set; }
    }
}
