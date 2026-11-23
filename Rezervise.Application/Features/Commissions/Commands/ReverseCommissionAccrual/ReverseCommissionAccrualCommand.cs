using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Commissions.Commands.ReverseCommissionAccrual
{
    /// <summary>
    /// Prim tahakkukunu ters çevirir (iptal/iade). Yalnızca Accrued
    /// durumundaki tahakkuklar ters çevrilebilir; ödenmiş (Paid)
    /// tahakkuklar reddedilir.
    /// OwnerId gövdeden değil JWT'den çözülür (Api katmanı set eder).
    /// </summary>
    public class ReverseCommissionAccrualCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        public Guid AccrualId { get; set; }
    }
}
