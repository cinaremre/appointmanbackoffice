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
    /// Kimlik doğrulama işlemlerini yönetir.
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
        /// Yeni kullanıcı kaydı oluşturur.
        /// </summary>
        /// <param name="command">Kayıt bilgileri</param>
        /// <returns>Oluşturulan kullanıcının ID'si</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kullanıcı girişi yapar ve token döndürür.
        /// </summary>
        /// <param name="command">Giriş bilgileri</param>
        /// <returns>Access token, refresh token ve kullanıcı bilgileri</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return Unauthorized(result);
        }

        /// <summary>
        /// Süresi dolan access token'i yeniler.
        /// </summary>
        /// <param name="command">Access token ve refresh token</param>
        /// <returns>Yeni access token ve refresh token</returns>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(Result<RefreshTokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<RefreshTokenResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return Unauthorized(result);
        }

        /// <summary>
        /// E-posta adresini, e-postadaki bağlantıdan gelen token ile doğrular.
        /// Bağlantı bir e-posta istemcisinden açıldığı için anonim erişime açıktır.
        /// </summary>
        /// <param name="token">Ham doğrulama token'i</param>
        /// <returns>Doğrulama sonucu</returns>
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
        /// GET varyantıyla aynı işlemi gövde üzerinden yapar (API istemcileri için).
        /// </summary>
        /// <param name="command">Token'i taşıyan komut</param>
        /// <returns>Doğrulama sonucu</returns>
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
        /// Doğrulama e-postasını yeniden gönderir. Yanıt, adresin sistemde kayıtlı
        /// olup olmadığını sızdırmamak için her durumda başarılı döner.
        /// </summary>
        /// <param name="command">E-posta adresini taşıyan komut</param>
        /// <returns>İstek kabul edildi bilgisi</returns>
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
        /// Kayıt sonrası gönderilen telefon doğrulama kodunu doğrular. Başarılı
        /// doğrulamada kullanıcı aktifleşir ve giriş yapabilir.
        /// </summary>
        /// <param name="command">Telefon numarası ve kod</param>
        /// <returns>Doğrulanan kullanıcının ID'si</returns>
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
        /// Telefon doğrulama kodunu yeniden gönderir. Yanıt, numaranın kayıtlı olup
        /// olmadığını sızdırmamak için her durumda başarılı döner.
        /// </summary>
        /// <param name="command">Telefon numarasını taşıyan komut</param>
        /// <returns>İstek kabul edildi bilgisi</returns>
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
        /// Doğrulama sonucunu uygun HTTP durum koduna eşler.
        /// İs kuralı ihlalleri (geçersiz/süresi geçmiş token) 400 döner.
        /// </summary>
        /// <param name="result">Komut sonucu</param>
        /// <returns>Sarılmış sonuç</returns>
        private IActionResult MapVerificationResult(Result<bool> result)
        {
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}