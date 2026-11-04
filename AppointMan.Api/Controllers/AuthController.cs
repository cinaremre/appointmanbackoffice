using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Users.Commands.Login;
using AppointMan.Application.Features.Users.Commands.RefreshToken;
using AppointMan.Application.Features.Users.Commands.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Kimlik dogrulama islemlerini yonetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Yeni kullanici kaydi olusturur.
        /// </summary>
        /// <param name="command">Kayit bilgileri</param>
        /// <returns>Olusturulan kullanicinin ID'si</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kullanici girisi yapar ve token dondurur.
        /// </summary>
        /// <param name="command">Giris bilgileri</param>
        /// <returns>Access token, refresh token ve kullanici bilgileri</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return Unauthorized(result);
        }

        /// <summary>
        /// Suresi dolan access token'i yeniler.
        /// </summary>
        /// <param name="command">Access token ve refresh token</param>
        /// <returns>Yeni access token ve refresh token</returns>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(Result<RefreshTokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<RefreshTokenResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return Unauthorized(result);
        }
    }
}