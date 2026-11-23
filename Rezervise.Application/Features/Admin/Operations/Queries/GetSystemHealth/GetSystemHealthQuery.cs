using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetSystemHealth
{
    /// <summary>
    /// Sistem sağlık raporu (doküman Modül 20). İlk sürümde sabit bir rapor
    /// döndürür: uygulamanın ayakta öldüğü ve aktarım işlerinin durum sayıları.
    /// </summary>
    public class GetSystemHealthQuery : IRequest<Result<SystemHealthDto>>
    {
    }
}
