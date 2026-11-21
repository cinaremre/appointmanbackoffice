using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Subscriptions.Commands.GrantFeatureOverrideByAdmin
{
    /// <summary>
    /// Backoffice ucu: isletmeye ozellik bazli gecici erisim karari verir
    /// (dokuman Modul 13). Is, mevcut GrantFeatureOverrideCommand uzerinden
    /// yurutulur; icerdeki handler admin rol kontrolunu ve workflow denetimini
    /// zaten yapar, bu sarmalayici yalnizca AdminAuditLogs'a yazar.
    /// </summary>
    public class GrantFeatureOverrideByAdminCommand : IRequest<Result<Guid>>
    {
        /// <summary>Route'tan gelir; govdeden gelen deger ezilir.</summary>
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
