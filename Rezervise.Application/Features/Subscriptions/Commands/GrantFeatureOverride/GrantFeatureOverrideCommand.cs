using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Subscriptions.Commands.GrantFeatureOverride
{
    /// <summary>
    /// İşletmeye özellik bazlı geçici erişim kararı verir (doküman Modül 14).
    /// Aktif override abonelik planındaki değerleri ezer.
    /// Yalnız platform yöneticileri çağırabilir.
    /// </summary>
    public class GrantFeatureOverrideCommand : IRequest<Result<Guid>>
    {
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
