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
    /// Musteri iletisim izinleri (dokuman Modul 16).
    /// Pazarlama gonderiminden once can-contact ucu zorunludur.
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
        /// Musterinin kanal + amac bazinda iznini belirler; gecmis kaydi ekler.
        /// Pazarlama izni geri cekilirse kanal tamamen kapatilir.
        /// </summary>
        /// <param name="command">Izin bilgileri</param>
        /// <returns>Guncellenen tercih kaydinin ID'si</returns>
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
        /// Belirtilen kanal + amac icin musterile iletisime gecilabilir mi?
        /// Pazarlama gonderimi oncesi cagrilmasi zorunludur.
        /// </summary>
        /// <param name="businessId">Isletme ID'si</param>
        /// <param name="customerId">Musteri User ID'si</param>
        /// <param name="channel">Iletisim kanali</param>
        /// <param name="purpose">Kullanim amaci</param>
        /// <returns>Iletisime gecilebilirse true</returns>
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
