using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Waitlist.Commands.JoinWaitlist
{
    /// <summary>
    /// Bekleme listesine yeni kayit (dokuman Modul 10). Musteri kendi
    /// adina kayit olursa CustomerId JWT kimliginden; isletme sahibi
    /// baskasi adina kayit acarsa CustomerId govdeden alinir.
    /// Ayni musteri + ayni hizmet icin aktif kayit varsa Conflict.
    /// </summary>
    public class JoinWaitlistCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Musteri ID. Isletme sahibi baskasi adina kayit acarken doludur;
        /// musteri kendi adina kayit olurken bos birakilabilir.</summary>
        public Guid CustomerId { get; set; }

        public Guid ServiceId { get; set; }

        /// <summary>Tercih edilen calisan; ilk surumde eslestirmede yok sayilir.</summary>
        public Guid? PreferredEmployeeId { get; set; }

        public DateTime EarliestDate { get; set; }
        public DateTime? LatestDate { get; set; }

        /// <summary>Tercih edilen gun ici saat araligi ("09:00-17:00" gibi). Opsiyonel.</summary>
        public string? PreferredTimeRange { get; set; }
    }
}
