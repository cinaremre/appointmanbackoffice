using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Imports.Commands.ExecuteImportJob
{
    /// <summary>
    /// Onay bekleyen aktarim isini kolon eslesmesi ile calistirir.
    /// ColumnMapping JSON formati: {"0":"FullName","1":"Phone"}.
    /// Tamamlanmis is tekrar calistirilamaz (Conflict).
    /// </summary>
    public class ExecuteImportJobCommand : IRequest<Result<Guid>>
    {
        public Guid ImportJobId { get; set; }

        /// <summary>Kolon index -> alan adi eslesmesi (JSON metni).</summary>
        public string ColumnMapping { get; set; } = string.Empty;
    }
}
