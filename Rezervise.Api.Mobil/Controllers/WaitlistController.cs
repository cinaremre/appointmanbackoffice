using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Waitlist.Commands.AcceptWaitlistOffer;
using Rezervise.Application.Features.Waitlist.Commands.CancelWaitlistEntry;
using Rezervise.Application.Features.Waitlist.Commands.CreateWaitlistOffers;
using Rezervise.Application.Features.Waitlist.Commands.JoinWaitlist;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Bekleme listesi işlemleri (doküman Modül 10, yarım otomatik mod):
    /// kayıt olma, işletmenin slot teklifi üretmesi, teklif kabul/iptal.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WaitlistController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public WaitlistController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Caller kimliği her zaman JWT'den çözülür; gövde değerleri yetki kaynağı değildir.
        /// </summary>
        private IActionResult? ResolveCurrentUserId(out Guid userId)
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                userId = Guid.Empty;
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Auth.InvalidUser")));
            }

            userId = _currentUser.UserId.Value;
            return null;
        }

        /// <summary>
        /// Bekleme listesine kayıt olur. Müşteri kendi adına; işletme sahibi
        /// isterse başkası adına kayıt açabilir.
        /// </summary>
        /// <param name="command">Kayıt bilgileri</param>
        /// <returns>Oluşturulan kaydın ID'si</returns>
        [HttpPost("join")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Join([FromBody] JoinWaitlistCommand command)
        {
            if (ResolveCurrentUserId(out _) is { } unauthorized)
                return unauthorized;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Açılan slot için uygun bekleme listesi kayıtlarına Pending teklifler
        /// üretir. Yetki: işletme sahibi (onay adımı).
        /// </summary>
        /// <param name="command">Slot ve teklif sayısı bilgileri</param>
        /// <returns>Üretilen tekliflerin ID listesi</returns>
        [HttpPost("offers")]
        [ProducesResponseType(typeof(Result<List<Guid>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<Guid>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOffers([FromBody] CreateWaitlistOffersCommand command)
        {
            if (ResolveCurrentUserId(out _) is { } unauthorized)
                return unauthorized;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Bekleme listesi teklifini kabul eder. Müşteri kimliği teklifin
        /// kaydından çözülür; gövdeye güvenilmez.
        /// </summary>
        /// <param name="command">Teklif bilgisi</param>
        /// <returns>Kabul edilen teklifin ID'si</returns>
        [HttpPost("accept")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Accept([FromBody] AcceptWaitlistOfferCommand command)
        {
            if (ResolveCurrentUserId(out _) is { } unauthorized)
                return unauthorized;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Bekleme listesi kaydını iptal eder. Yetki: müşterinin kendisi
        /// veya işletme sahibi.
        /// </summary>
        /// <param name="command">Kayıt bilgisi</param>
        /// <returns>İptal edilen kaydın ID'si</returns>
        [HttpPost("cancel")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cancel([FromBody] CancelWaitlistEntryCommand command)
        {
            if (ResolveCurrentUserId(out _) is { } unauthorized)
                return unauthorized;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
