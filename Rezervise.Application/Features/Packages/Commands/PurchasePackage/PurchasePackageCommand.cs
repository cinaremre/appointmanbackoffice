using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Packages.Commands.PurchasePackage
{
    /// <summary>
    /// Müşteriye paket satışı kaydeder (doküman Modül 5). Geçerlilik bitişi
    /// satın alma anından itibaren paketin ValidityDays gün sonrasıdır.
    /// Yetki: işletme sahibi satışı kaydeder.
    /// </summary>
    public class PurchasePackageCommand : IRequest<Result<Guid>>
    {
        public Guid ServicePackageId { get; set; }
        public Guid CustomerId { get; set; }
    }
}
