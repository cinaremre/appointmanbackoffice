using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetSystemHealth
{
    /// <summary>
    /// Sistem saglik raporu (dokuman Modul 20). Ilk surumde sabit bir rapor
    /// dondurur: uygulamanin ayakta oldugu ve aktarim islerinin durum sayilari.
    /// </summary>
    public class GetSystemHealthQuery : IRequest<Result<SystemHealthDto>>
    {
    }
}
