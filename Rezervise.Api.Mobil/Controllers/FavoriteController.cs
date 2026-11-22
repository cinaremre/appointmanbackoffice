using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Favorites.Commands.AddFavorite;
using Rezervise.Application.Features.Favorites.Commands.RemoveFavorite;
using Rezervise.Application.Features.Favorites.Queries.CheckFavorite;
using Rezervise.Application.Features.Favorites.Queries.GetByUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Kullanici favorileri ile ilgili islemleri yonetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoriteController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public FavoriteController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Kullanici ID'si her zaman JWT'den cozulur; govde UserId degeri yetki kaynagi degildir.
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
        /// Kullanicinin favorilerini getirir.
        /// </summary>
        /// <param name="userId">Kullanici ID'si</param>
        /// <param name="pageNumber">Sayfa numarasi (varsayilan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayilan: 10)</param>
        /// <returns>Favori listesi</returns>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(Result<List<FavoriteDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<FavoriteDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByUser(
            Guid userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // 1. Query'yi olustur
            var query = new GetFavoritesByUserQuery
            {
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Bir isletmenin favori olup olmadigini kontrol eder.
        /// </summary>
        /// <param name="userId">Kullanici ID'si</param>
        /// <param name="businessId">Isletme ID'si</param>
        /// <returns>Favori durumu (true/false)</returns>
        [HttpGet("check")]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Check([FromQuery] Guid userId, [FromQuery] Guid businessId)
        {
            // 1. Query'yi olustur
            var query = new CheckFavoriteQuery { UserId = userId, BusinessId = businessId };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Favoriye ekler.
        /// </summary>
        /// <param name="command">Favori bilgileri</param>
        /// <returns>Olusturulan favorinin ID'si</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Add([FromBody] AddFavoriteCommand command)
        {
            // 1. UserId JWT'den gelir; govde degerine guvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.UserId = userId;

            // 2. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Favoriden cikarir.
        /// </summary>
        /// <param name="command">Cikarma bilgileri</param>
        /// <returns>Silinen favorinin ID'si</returns>
        [HttpDelete]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Remove([FromBody] RemoveFavoriteCommand command)
        {
            // 1. UserId JWT'den gelir; govde degerine guvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.UserId = userId;

            // 2. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}