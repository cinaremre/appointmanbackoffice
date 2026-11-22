using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Subscriptions.Commands.GrantFeatureOverride
{
    /// <summary>
    /// Isletmeye ozellik bazli gecici erisim karari verir (dokuman Modul 14).
    /// Aktif override abonelik planindaki degerleri ezer.
    /// Yalniz platform yoneticileri cagirabilir.
    /// </summary>
    public class GrantFeatureOverrideCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid FeatureId { get; set; }
        public bool IsEnabled { get; set; }

        /// <summary>Null ise plan kotasi gecerli kalir.</summary>
        public int? QuotaOverride { get; set; }

        /// <summary>Gecerlilik baslangici (UTC).</summary>
        public DateTime ValidFrom { get; set; }

        /// <summary>Gecerlilik sonu (UTC); null = belirsiz sure.</summary>
        public DateTime? ValidUntil { get; set; }

        /// <summary>Kayit gerekcesi (denetim icin onerilir).</summary>
        public string? Reason { get; set; }
    }
}
