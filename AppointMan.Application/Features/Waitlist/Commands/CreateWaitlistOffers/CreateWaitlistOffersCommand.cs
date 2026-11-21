using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Waitlist.Commands.CreateWaitlistOffers
{
    /// <summary>
    /// Isletme onay adimi (dokuman Modul 10): acilan bir slot icin uygun
    /// bekleme listesi kayitlarina Pending teklifler uretir. Once kayit
    /// olan musteri once teklif alir (CreatedAt sirali). Teklif suresi
    /// <see cref="Domain.Common.WaitlistPolicy.OfferLifetime"/> kadardir.
    /// Yetki: isletme sahibi.
    /// </summary>
    public class CreateWaitlistOffersCommand : IRequest<Result<List<Guid>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Acilan slotun baslangic zamani (randevu saati, UTC).</summary>
        public DateTime SlotStart { get; set; }

        public Guid ServiceId { get; set; }

        /// <summary>Bu slot icin uretilecek en fazla teklif sayisi.</summary>
        public int MaxOffers { get; set; }
    }
}
