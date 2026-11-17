using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Users.Commands.ChangePassword;
using AppointMan.Application.Features.Users.Commands.DeleteAccount;
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
        private readonly ICurrentUserService _currentUser;

        public UserController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
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
            // The actor is always taken from the token: a client cannot change another user's password.
            var currentUserId = _currentUser.UserId;
            if (currentUserId is null)
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Auth.InvalidUser")));

            command.UserId = currentUserId.Value;

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
            // The actor is always taken from the token: a client cannot edit another user's profile.
            var currentUserId = _currentUser.UserId;
            if (currentUserId is null)
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Auth.InvalidUser")));

            command.UserId = currentUserId.Value;

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
            // The actor is always taken from the token: a client cannot change another user's language.
            var currentUserId = _currentUser.UserId;
            if (currentUserId is null)
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Auth.InvalidUser")));

            command.UserId = currentUserId.Value;

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
            var currentUserId = _currentUser.UserId;
            if (currentUserId is null)
                return Unauthorized(Result<UserDto>.Failure(Error.Unauthorized("Auth.InvalidUser")));

            // BOLA protection: a caller may only read their own profile unless they are an administrator.
            var isAdministrator = _currentUser.IsInRole("Admin") || _currentUser.IsInRole("SuperAdmin");
            if (id != currentUserId.Value && !isAdministrator)
                return NotFound(Result<UserDto>.Failure(Error.NotFound("User", id.ToString())));

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
            // The verified account is always the caller; the body cannot target another user.
            var currentUserId = _currentUser.UserId;
            if (currentUserId is null)
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Auth.InvalidUser")));

            command.UserId = currentUserId.Value;

            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kullanicinin kendi hesabini siler ve kisisel bilgilerini anonimlestirir.
        /// Aktor her zaman token'dan cozulur; istek govdesinden kullanici ID'si kabul edilmez.
        /// </summary>
        /// <param name="command">Mevcut sifreyi tasiyan silme istegi</param>
        /// <returns>Islem sonucu</returns>
        [HttpPost("delete-account")]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountCommand command)
        {
            // 1. Command'i mediator ile gonder (kimlik handler icinde token'dan cozulur)
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return result.Error?.Code switch
            {
                "NOT_FOUND" => NotFound(result),
                "UNAUTHORIZED" => Unauthorized(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }
    }
}