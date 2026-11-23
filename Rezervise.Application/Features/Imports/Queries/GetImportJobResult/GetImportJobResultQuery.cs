using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Imports.Queries.GetImportJobResult
{
    /// <summary>
    /// Aktarım işinin özetini ve satır bazlı sonuçlarını getirir.
    /// </summary>
    public class GetImportJobResultQuery : IRequest<Result<ImportJobResultDto>>
    {
        public Guid ImportJobId { get; set; }
    }
}
