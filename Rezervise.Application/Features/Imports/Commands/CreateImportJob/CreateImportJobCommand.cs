using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Imports.Commands.CreateImportJob
{
    /// <summary>
    /// Musteri CSV dosyasi icin aktarim isi olusturur.
    /// Satirlar parse edilip sayilir; satir kayitlari (ImportRowResult)
    /// bu asamada olusturulmaz, is kolon eslesmesi (Mapping) bekler.
    /// </summary>
    public class CreateImportJobCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Kaynak sistem etiketi (orn. "google-contacts", "excel").</summary>
        public string SourceSystem { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;

        /// <summary>Ham CSV icerigi (RFC4180; tirnakli alanlar desteklenir).</summary>
        public string CsvContent { get; set; } = string.Empty;
    }
}
