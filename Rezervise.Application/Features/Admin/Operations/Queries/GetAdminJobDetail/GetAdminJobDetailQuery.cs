using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobDetail
{
    /// <summary>
    /// Tek bir aktarım işinin detayı: is özeti ve satır sonuçlarının ilk 200'i.
    /// </summary>
    public class GetAdminJobDetailQuery : IRequest<Result<AdminJobDetailDto>>
    {
        public Guid ImportJobId { get; set; }
    }
}
