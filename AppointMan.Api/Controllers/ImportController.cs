using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Imports.Commands.CreateImportJob;
using AppointMan.Application.Features.Imports.Commands.ExecuteImportJob;
using AppointMan.Application.Features.Imports.Queries.GetImportJobResult;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Musteri verisi aktarimi (CSV). Isletme sahibi dosya yukler,
    /// kolon eslesmesi ile islemi onaylar, sonucu sorgular.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ImportController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ImportController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// CSV dosyasi icin aktarim isi olusturur. Satirlar parse edilir,
        /// is kolon eslesmesi (onay) bekler; satir kayitlari olusmaz.
        /// </summary>
        /// <param name="command">Dosya ve CSV icerigi</param>
        /// <returns>Aktarim isi ID'si</returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateImportJobCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Aktarim isini kolon eslesmesi ile calistirir. Ayni telefon
        /// (SourceRecordKey) tekrarlari SkippedDuplicate olarak atlanir.
        /// Tamamlanmis is tekrar calistirilamaz (Conflict).
        /// </summary>
        /// <param name="command">Is ID'si ve kolon eslesmesi (JSON)</param>
        /// <returns>Aktarim isi ID'si</returns>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Execute([FromBody] ExecuteImportJobCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Aktarim isinin ozetini ve satir bazli sonuclarini getirir.
        /// </summary>
        /// <param name="importJobId">Aktarim isi ID'si</param>
        /// <returns>Ozet ve satir sonuclari</returns>
        [HttpGet("result/{importJobId}")]
        [ProducesResponseType(typeof(Result<ImportJobResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<ImportJobResultDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResult(Guid importJobId)
        {
            // 1. Query'yi olustur
            var query = new GetImportJobResultQuery { ImportJobId = importJobId };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}
