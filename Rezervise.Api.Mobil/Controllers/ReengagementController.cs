using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Reengagement.Commands.CreateReengagementRule;
using Rezervise.Application.Features.Reengagement.Commands.MarkReengagementNotified;
using Rezervise.Application.Features.Reengagement.Queries.GetReengagementCandidates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Kural bazli musteri geri cagirma (dokuman Modul 13, ilk surum).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReengagementController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReengagementController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Isletme icin yeni geri cagirma kurali olusturur.
        /// </summary>
        /// <param name="command">Kural bilgileri</param>
        /// <returns>Olusturulan kuralin ID'si</returns>
        [HttpPost("rule")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRule([FromBody] CreateReengagementRuleCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Isletmenin aktif geri cagirma adaylarini listeler.
        /// </summary>
        /// <param name="businessId">Isletme ID'si</param>
        /// <returns>Aday listesi</returns>
        [HttpGet("candidates/{businessId}")]
        [ProducesResponseType(typeof(Result<List<ReengagementCandidateDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<ReengagementCandidateDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCandidates(Guid businessId)
        {
            var query = new GetReengagementCandidatesQuery { BusinessId = businessId };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Aday listesinden secilen musteri icin bildirim gonderildi isareti ekler.
        /// </summary>
        /// <param name="command">Isaretleme bilgileri</param>
        /// <returns>Olusturulan deneme kaydinin ID'si</returns>
        [HttpPost("mark-notified")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkNotified([FromBody] MarkReengagementNotifiedCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
