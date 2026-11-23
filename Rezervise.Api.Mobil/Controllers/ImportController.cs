using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Imports.Commands.CreateImportJob;
using Rezervise.Application.Features.Imports.Commands.ExecuteImportJob;
using Rezervise.Application.Features.Imports.Queries.GetImportJobResult;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Müşteri verişi aktarımı (CSV). İşletme sahibi dosya yükler,
    /// kolon eşleşmesi ile işlemi onaylar, sonucu sorgular.
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
        /// CSV dosyası için aktarım isi oluşturur. Satırlar parse edilir,
        /// is kolon eşleşmesi (onay) bekler; satır kayıtları oluşmaz.
        /// </summary>
        /// <param name="command">Dosya ve CSV içeriği</param>
        /// <returns>Aktarım isi ID'si</returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateImportJobCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Aktarım işini kolon eşleşmesi ile çalıştırır. Aynı telefon
        /// (SourceRecordKey) tekrarları SkippedDuplicate olarak atlanır.
        /// Tamamlanmış is tekrar çalıştırılamaz (Conflict).
        /// </summary>
        /// <param name="command">İs ID'si ve kolon eşleşmesi (JSON)</param>
        /// <returns>Aktarım isi ID'si</returns>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Execute([FromBody] ExecuteImportJobCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Aktarım işinin özetini ve satır bazlı sonuçlarını getirir.
        /// </summary>
        /// <param name="importJobId">Aktarım isi ID'si</param>
        /// <returns>Özet ve satır sonuçları</returns>
        [HttpGet("result/{importJobId}")]
        [ProducesResponseType(typeof(Result<ImportJobResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<ImportJobResultDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResult(Guid importJobId)
        {
            // 1. Query'yi oluştur
            var query = new GetImportJobResultQuery { ImportJobId = importJobId };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}
