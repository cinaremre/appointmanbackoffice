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
    /// Kural bazlı müşteri geri çağırma (doküman Modül 13, ilk sürüm).
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
        /// İşletme için yeni geri çağırma kuralı oluşturur.
        /// </summary>
        /// <param name="command">Kural bilgileri</param>
        /// <returns>Oluşturulan kuralın ID'si</returns>
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
        /// İşletmenin aktif geri çağırma adaylarını listeler.
        /// </summary>
        /// <param name="businessId">İşletme ID'si</param>
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
        /// Aday listesinden seçilen müşteri için bildirim gönderildi işareti ekler.
        /// </summary>
        /// <param name="command">İşaretleme bilgileri</param>
        /// <returns>Oluşturulan deneme kaydının ID'si</returns>
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
