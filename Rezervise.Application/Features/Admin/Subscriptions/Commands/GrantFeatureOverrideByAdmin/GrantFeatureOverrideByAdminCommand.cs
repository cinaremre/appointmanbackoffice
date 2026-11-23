using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Commands.GrantFeatureOverrideByAdmin
{
    /// <summary>
    /// Backoffice ucu: işletmeye özellik bazlı geçici erişim kararı verir
    /// (doküman Modül 13). İs, mevcut GrantFeatureOverrideCommand üzerinden
    /// yürütülür; içerdeki handler admin rol kontrolünü ve workflow denetimini
    /// zaten yapar, bu sarmalayıcı yalnızca AdminAuditLogs'a yazar.
    /// </summary>
    public class GrantFeatureOverrideByAdminCommand : IRequest<Result<Guid>>
    {
        /// <summary>Route'tan gelir; gövdeden gelen değer ezilir.</summary>
        public Guid BusinessId { get; set; }

        public Guid FeatureId { get; set; }
        public bool IsEnabled { get; set; }

        /// <summary>Null ise plan kotası geçerli kalır.</summary>
        public int? QuotaOverride { get; set; }

        /// <summary>Geçerlilik başlangıcı (UTC).</summary>
        public DateTime ValidFrom { get; set; }

        /// <summary>Geçerlilik sonu (UTC); null = belirsiz süre.</summary>
        public DateTime? ValidUntil { get; set; }

        /// <summary>Kayıt gerekçesi (denetim için önerilir).</summary>
        public string? Reason { get; set; }
    }
}
