using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Packages.Commands.PurchasePackage
{
    /// <summary>
    /// Musteriye paket satisi kaydeder (dokuman Modul 5). Gecerlilik bitisi
    /// satin alma anindan itibaren paketin ValidityDays gun sonrasidir.
    /// Yetki: isletme sahibi satisi kaydeder.
    /// </summary>
    public class PurchasePackageCommand : IRequest<Result<Guid>>
    {
        public Guid ServicePackageId { get; set; }
        public Guid CustomerId { get; set; }
    }
}
