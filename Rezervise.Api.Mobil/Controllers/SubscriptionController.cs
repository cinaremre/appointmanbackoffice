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
    /// Abonelik ve özellik entitlement yönetimi (doküman Modül 14).
    /// Uçların tamamı platform yöneticisine açıktır; handler'larin içinde
    /// de rol kontrolü yapılır, buradaki attribute savunma ikinci katmanıdır.
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
        /// Yeni abonelik planı oluşturur ve özellik setini bağlar.
        /// </summary>
        /// <param name="command">Plan bilgileri ve özellik listesi</param>
        /// <returns>Oluşturulan planın ID'si</returns>
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
        /// İşletmeye abonelik planı atar; eski aktif abonelik düşürülür.
        /// </summary>
        /// <param name="command">Atama bilgileri</param>
        /// <returns>Oluşturulan aboneliğin ID'si</returns>
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
        /// İşletmeye özellik bazlı geçici erişim kararı (override) verir.
        /// </summary>
        /// <param name="command">Override bilgileri</param>
        /// <returns>Oluşturulan override kaydının ID'si</returns>
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
