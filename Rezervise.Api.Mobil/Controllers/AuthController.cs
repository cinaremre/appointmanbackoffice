using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Users.Commands.Login;
using Rezervise.Application.Features.Users.Commands.RefreshToken;
using Rezervise.Application.Features.Users.Commands.RegisterUser;
using Rezervise.Application.Features.Users.Commands.ResendPhoneOtp;
using Rezervise.Application.Features.Users.Commands.ResendVerificationEmail;
using Rezervise.Application.Features.Users.Commands.VerifyEmailToken;
using Rezervise.Application.Features.Users.Commands.VerifyPhoneOtp;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
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

        /// <summary>
        /// E-posta adresini, e-postadaki baglantidan gelen token ile dogrular.
        /// Baglanti bir e-posta istemcisinden acildigi icin anonim erisime aciktir.
        /// </summary>
        /// <param name="token">Ham dogrulama token'i</param>
        /// <returns>Dogrulama sonucu</returns>
        [AllowAnonymous]
        [HttpGet("verify-email")]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var result = await _mediator.Send(new VerifyEmailTokenCommand { Token = token });

            return MapVerificationResult(result);
        }

        /// <summary>
        /// GET varyantiyla ayni islemi govde uzerinden yapar (API istemcileri icin).
        /// </summary>
        /// <param name="command">Token'i tasiyan komut</param>
        /// <returns>Dogrulama sonucu</returns>
        [AllowAnonymous]
        [HttpPost("verify-email")]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmailPost([FromBody] VerifyEmailTokenCommand command)
        {
            var result = await _mediator.Send(command);

            return MapVerificationResult(result);
        }

        /// <summary>
        /// Dogrulama e-postasini yeniden gonderir. Yanit, adresin sistemde kayitli
        /// olup olmadigini sizdirmamak icin her durumda basarili doner.
        /// </summary>
        /// <param name="command">E-posta adresini tasiyan komut</param>
        /// <returns>Istek kabul edildi bilgisi</returns>
        [AllowAnonymous]
        [HttpPost("resend-verification")]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationEmailCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kayit sonrasi gonderilen telefon dogrulama kodunu dogrular. Basarili
        /// dogrulamada kullanici aktiflesir ve giris yapabilir.
        /// </summary>
        /// <param name="command">Telefon numarasi ve kod</param>
        /// <returns>Dogrulanan kullanicinin ID'si</returns>
        [AllowAnonymous]
        [HttpPost("verify-phone")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneOtpCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Telefon dogrulama kodunu yeniden gonderir. Yanit, numaranin kayitli olup
        /// olmadigini sizdirmamak icin her durumda basarili doner.
        /// </summary>
        /// <param name="command">Telefon numarasini tasiyan komut</param>
        /// <returns>Istek kabul edildi bilgisi</returns>
        [AllowAnonymous]
        [HttpPost("resend-phone-code")]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendPhoneCode([FromBody] ResendPhoneOtpCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Dogrulama sonucunu uygun HTTP durum koduna esler.
        /// Is kurali ihlalleri (gecersiz/suresi gecmis token) 400 doner.
        /// </summary>
        /// <param name="result">Komut sonucu</param>
        /// <returns>Sarilmis sonuc</returns>
        private IActionResult MapVerificationResult(Result<bool> result)
        {
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}