using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Subscriptions.Commands.AssignSubscription;
using Rezervise.Application.Features.Subscriptions.Commands.CreatePlan;
using Rezervise.Application.Features.Subscriptions.Commands.GrantFeatureOverride;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Abonelik ve ozellik entitlement yonetimi (dokuman Modul 14).
    /// Uclarin tamami platform yoneticisine aciktir; handler'larin icinde
    /// de rol kontrolu yapilir, buradaki attribute savunma ikinci katmanidir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SubscriptionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Yeni abonelik plani olusturur ve ozellik setini baglar.
        /// </summary>
        /// <param name="command">Plan bilgileri ve ozellik listesi</param>
        /// <returns>Olusturulan planin ID'si</returns>
        [HttpPost("plan")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            if (result.Error!.Code == "FORBIDDEN")
                return Forbid();

            return BadRequest(result);
        }

        /// <summary>
        /// Isletmeye abonelik plani atar; eski aktif abonelik dusurulur.
        /// </summary>
        /// <param name="command">Atama bilgileri</param>
        /// <returns>Olusturulan aboneligin ID'si</returns>
        [HttpPost("assign")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Assign([FromBody] AssignSubscriptionCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            if (result.Error!.Code == "FORBIDDEN")
                return Forbid();

            return BadRequest(result);
        }

        /// <summary>
        /// Isletmeye ozellik bazli gecici erisim karari (override) verir.
        /// </summary>
        /// <param name="command">Override bilgileri</param>
        /// <returns>Olusturulan override kaydinin ID'si</returns>
        [HttpPost("override")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GrantOverride([FromBody] GrantFeatureOverrideCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            if (result.Error!.Code == "FORBIDDEN")
                return Forbid();

            return BadRequest(result);
        }
    }
}
