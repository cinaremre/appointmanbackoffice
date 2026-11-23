using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Imports.Commands.ExecuteImportJob
{
    /// <summary>
    /// Onay bekleyen aktarım işini kolon eşleşmesi ile çalıştırır.
    /// ColumnMapping JSON formatı: {"0":"FullName","1":"Phone"}.
    /// Tamamlanmış is tekrar çalıştırılamaz (Conflict).
    /// </summary>
    public class ExecuteImportJobCommand : IRequest<Result<Guid>>
    {
        public Guid ImportJobId { get; set; }

        /// <summary>Kolon index -> alan adı eşleşmesi (JSON metni).</summary>
        public string ColumnMapping { get; set; } = string.Empty;
    }
}
