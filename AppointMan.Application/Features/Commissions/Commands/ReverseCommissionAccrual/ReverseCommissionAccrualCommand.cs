using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Commissions.Commands.ReverseCommissionAccrual
{
    /// <summary>
    /// Prim tahakkukunu ters cevirir (iptal/iade). Yalnizca Accrued
    /// durumundaki tahakkuklar ters cevrilebilir; odenmis (Paid)
    /// tahakkuklar reddedilir.
    /// OwnerId govdeden degil JWT'den cozulur (Api katmani set eder).
    /// </summary>
    public class ReverseCommissionAccrualCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        public Guid AccrualId { get; set; }
    }
}
