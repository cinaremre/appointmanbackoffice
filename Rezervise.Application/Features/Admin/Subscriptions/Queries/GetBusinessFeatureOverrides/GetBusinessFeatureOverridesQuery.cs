using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Queries.GetBusinessFeatureOverrides
{
    /// <summary>
    /// İşletmeye verilmiş özellik override kararları listeler
    /// (doküman Modül 13). Geçmiş kayıtlar da görünür; yönetici arayüzü
    /// geçerlilik aralığından aktif olanları seçer.
    /// </summary>
    public class GetBusinessFeatureOverridesQuery : IRequest<Result<IReadOnlyList<AdminFeatureOverrideDto>>>
    {
        public Guid BusinessId { get; set; }
    }

    /// <summary>Tek override karanın admin görünümü.</summary>
    public class AdminFeatureOverrideDto
    {
        public Guid Id { get; set; }
        public Guid FeatureId { get; set; }
        public string FeatureCode { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }

        /// <summary>Null ise plan kotası geçerli kalır.</summary>
        public int? QuotaOverride { get; set; }

        public DateTime ValidFrom { get; set; }

        /// <summary>UTC geçerlilik sonu; null = belirsiz süre.</summary>
        public DateTime? ValidUntil { get; set; }

        public string? Reason { get; set; }

        /// <summary>Kararı veren platform yöneticisi.</summary>
        public Guid GrantedBy { get; set; }
    }
}
