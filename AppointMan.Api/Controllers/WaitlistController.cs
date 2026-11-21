using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Waitlist.Commands.AcceptWaitlistOffer;
using AppointMan.Application.Features.Waitlist.Commands.CancelWaitlistEntry;
using AppointMan.Application.Features.Waitlist.Commands.CreateWaitlistOffers;
using AppointMan.Application.Features.Waitlist.Commands.JoinWaitlist;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Bekleme listesi islemleri (dokuman Modul 10, yarim otomatik mod):
    /// kayit olma, isletmenin slot teklifi uretmesi, teklif kabul/iptal.
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
        /// Caller kimligi her zaman JWT'den cozulur; govde degerleri yetki kaynagi degildir.
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
        /// Bekleme listesine kayit olur. Musteri kendi adina; isletme sahibi
        /// isterse baskasi adina kayit acabilir.
        /// </summary>
        /// <param name="command">Kayit bilgileri</param>
        /// <returns>Olusturulan kaydin ID'si</returns>
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
        /// Acilan slot icin uygun bekleme listesi kayitlarina Pending teklifler
        /// uretir. Yetki: isletme sahibi (onay adimi).
        /// </summary>
        /// <param name="command">Slot ve teklif sayisi bilgileri</param>
        /// <returns>Uretilen tekliflerin ID listesi</returns>
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
        /// Bekleme listesi teklifini kabul eder. Musteri kimligi teklifin
        /// kaydindan cozulur; govdeye guvenilmez.
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
        /// Bekleme listesi kaydini iptal eder. Yetki: musterinin kendisi
        /// veya isletme sahibi.
        /// </summary>
        /// <param name="command">Kayit bilgisi</param>
        /// <returns>Iptal edilen kaydin ID'si</returns>
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
