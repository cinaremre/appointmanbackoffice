using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Users.Commands.ChangePassword;
using AppointMan.Application.Features.Users.Commands.UpdateUserProfile;
using AppointMan.Application.Features.Users.Commands.UpdateUserLanguage;
using AppointMan.Application.Features.Users.Commands.VerifyEmail;
using AppointMan.Application.Features.Users.Queries.GetUserByEmail;
using AppointMan.Application.Features.Users.Queries.GetUserById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Kullanici islemlerini yonetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Kullanicinin sifresini degistirir.
        /// </summary>
        /// <param name="command">Sifre degistirme bilgileri</param>
        /// <returns>Islem sonucu</returns>
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kullanicinin profil bilgilerini gunceller.
        /// </summary>
        /// <param name="command">Profil guncelleme bilgileri</param>
        /// <returns>Islem sonucu</returns>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kullanicinin dil tercihini gunceller. Sonraki isteklerde donen
        /// metinler bu dile gore cozulur.
        /// </summary>
        /// <param name="command">Dil guncelleme bilgileri</param>
        /// <returns>Islem sonucu</returns>
        [HttpPut("language")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLanguage([FromBody] UpdateUserLanguageCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// ID'ye gore kullanici getirir.
        /// </summary>
        /// <param name="id">Kullanici ID'si</param>
        /// <returns>Kullanici detayi</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            // 1. Query'yi olustur
            var query = new GetUserByIdQuery { UserId = id };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Email'e gore kullanici getirir. (Sadece Admin)
        /// </summary>
        /// <param name="email">Kullanici email adresi</param>
        /// <returns>Kullanici detayi</returns>
        [HttpGet("email/{email}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            // 1. Query'yi olustur
            var query = new GetUserByEmailQuery { Email = email };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Kullanicinin email adresini dogrular.
        /// </summary>
        /// <param name="command">Dogrulama bilgileri</param>
        /// <returns>Islem sonucu</returns>
        [HttpPost("verify-email")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}