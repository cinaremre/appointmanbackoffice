using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Imports.Queries.GetImportJobResult
{
    /// <summary>
    /// Aktarim isinin ozetini ve satir bazli sonuclarini getirir.
    /// </summary>
    public class GetImportJobResultQuery : IRequest<Result<ImportJobResultDto>>
    {
        public Guid ImportJobId { get; set; }
    }
}
