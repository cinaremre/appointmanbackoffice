using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Imports.Commands.CreateImportJob
{
    /// <summary>
    /// Müşteri CSV dosyası için aktarım isi oluşturur.
    /// Satırlar parse edilip sayılır; satır kayıtları (ImportRowResult)
    /// bu aşamada oluşturulmaz, is kolon eşleşmesi (Mapping) bekler.
    /// </summary>
    public class CreateImportJobCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Kaynak sistem etiketi (orn. "google-contacts", "excel").</summary>
        public string SourceSystem { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;

        /// <summary>Ham CSV içeriği (RFC4180; tırnaklı alanlar desteklenir).</summary>
        public string CsvContent { get; set; } = string.Empty;
    }
}
