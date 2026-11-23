using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Waitlist.Commands.JoinWaitlist
{
    /// <summary>
    /// Bekleme listesine yeni kayıt (doküman Modül 10). Müşteri kendi
    /// adına kayıt olursa CustomerId JWT kimliğinden; işletme sahibi
    /// başkası adına kayıt açarsa CustomerId gövdeden alınır.
    /// Aynı müşteri + aynı hizmet için aktif kayıt varsa Conflict.
    /// </summary>
    public class JoinWaitlistCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Müşteri ID. İşletme sahibi başkası adına kayıt açarken doludur;
        /// müşteri kendi adına kayıt olurken boş bırakılabilir.</summary>
        public Guid CustomerId { get; set; }

        public Guid ServiceId { get; set; }

        /// <summary>Tercih edilen çalışan; ilk sürümde eşleştirmede yok sayılır.</summary>
        public Guid? PreferredEmployeeId { get; set; }

        public DateTime EarliestDate { get; set; }
        public DateTime? LatestDate { get; set; }

        /// <summary>Tercih edilen gün içi saat aralığı ("09:00-17:00" gibi). Opsiyonel.</summary>
        public string? PreferredTimeRange { get; set; }
    }
}
