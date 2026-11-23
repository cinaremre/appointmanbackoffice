using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Consents.Commands.SetCommunicationConsent;
using Rezervise.Application.Features.Consents.Queries.CanContact;
using Rezervise.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Müşteri iletişim izinleri (doküman Modül 16).
    /// Pazarlama gönderiminden önce can-contact ucu zorunludur.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConsentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ConsentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Müşterinin kanal + amaç bazında iznini belirler; geçmiş kaydı ekler.
        /// Pazarlama izni geri çekilirse kanal tamamen kapatılır.
        /// </summary>
        /// <param name="command">İzin bilgileri</param>
        /// <returns>Güncellenen tercih kaydının ID'si</returns>
        [HttpPost("set")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Set([FromBody] SetCommunicationConsentCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Belirtilen kanal + amaç için müşterile iletişime geçilabilir mi?
        /// Pazarlama gönderimi öncesi çağrılması zorunludur.
        /// </summary>
        /// <param name="businessId">İşletme ID'si</param>
        /// <param name="customerId">Müşteri User ID'si</param>
        /// <param name="channel">İletişim kanalı</param>
        /// <param name="purpose">Kullanım amacı</param>
        /// <returns>İletişime geçilebilirse true</returns>
        [HttpGet("can-contact")]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CanContact(
            Guid businessId,
            Guid customerId,
            [FromQuery] EnumConsentChannel channel,
            [FromQuery] EnumConsentPurpose purpose)
        {
            var query = new CanContactQuery
            {
                BusinessId = businessId,
                CustomerId = customerId,
                Channel = channel,
                Purpose = purpose
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
