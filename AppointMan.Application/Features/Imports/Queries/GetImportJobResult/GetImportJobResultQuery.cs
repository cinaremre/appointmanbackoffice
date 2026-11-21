using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Imports.Queries.GetImportJobResult
{
    /// <summary>
    /// Aktarim isinin ozetini ve satir bazli sonuclarini getirir.
    /// </summary>
    public class GetImportJobResultQuery : IRequest<Result<ImportJobResultDto>>
    {
        public Guid ImportJobId { get; set; }
    }
}
