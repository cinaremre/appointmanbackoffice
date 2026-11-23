using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Waitlist.Commands.CreateWaitlistOffers
{
    /// <summary>
    /// İşletme onay adımı (doküman Modül 10): açılan bir slot için uygun
    /// bekleme listesi kayıtlarına Pending teklifler üretir. Önce kayıt
    /// olan müşteri önce teklif alır (CreatedAt sıralı). Teklif süresi
    /// <see cref="Domain.Common.WaitlistPolicy.OfferLifetime"/> kadardır.
    /// Yetki: işletme sahibi.
    /// </summary>
    public class CreateWaitlistOffersCommand : IRequest<Result<List<Guid>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Açılan slotun başlangıç zamanı (randevu saati, UTC).</summary>
        public DateTime SlotStart { get; set; }

        public Guid ServiceId { get; set; }

        /// <summary>Bu slot için üretilecek en fazla teklif sayısı.</summary>
        public int MaxOffers { get; set; }
    }
}
