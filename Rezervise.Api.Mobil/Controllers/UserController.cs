using Rezervise.Api.Authorization;
using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Users.Commands.ChangePassword;
using Rezervise.Application.Features.Users.Commands.DeleteAccount;
using Rezervise.Application.Features.Users.Commands.UpdateUserProfile;
using Rezervise.Application.Features.Users.Commands.UpdateUserLanguage;
using Rezervise.Application.Features.Users.Commands.VerifyEmail;
using Rezervise.Application.Features.Users.Queries.GetUserByEmail;
using Rezervise.Application.Features.Users.Queries.GetUserById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Kullanıcı işlemlerini yönetir.
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
        /// Kullanıcının şifresini değiştirir.
        /// </summary>
        /// <param name="command">Şifre değiştirme bilgileri</param>
        /// <returns>İşlem sonucu</returns>
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

            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kullanıcının profil bilgilerini günceller.
        /// </summary>
        /// <param name="command">Profil güncelleme bilgileri</param>
        /// <returns>İşlem sonucu</returns>
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

            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kullanıcının dil tercihini günceller. Sonraki isteklerde dönen
        /// metinler bu dile göre çözülür.
        /// </summary>
        /// <param name="command">Dil güncelleme bilgileri</param>
        /// <returns>İşlem sonucu</returns>
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

            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// ID'ye göre kullanıcı getirir.
        /// </summary>
        /// <param name="id">Kullanıcı ID'si</param>
        /// <returns>Kullanıcı detayı</returns>
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

            // 1. Query'yi oluştur
            var query = new GetUserByIdQuery { UserId = id };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Email'e göre kullanıcı getirir. (Sadece Admin)
        /// </summary>
        /// <param name="email">Kullanıcı email adresi</param>
        /// <returns>Kullanıcı detayı</returns>
        [HttpGet("email/{email}")]
        [Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
        [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<UserDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            // 1. Query'yi oluştur
            var query = new GetUserByEmailQuery { Email = email };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Kullanıcının email adresini doğrular.
        /// </summary>
        /// <param name="command">Doğrulama bilgileri</param>
        /// <returns>İşlem sonucu</returns>
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

            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kullanıcının kendi hesabını siler ve kişisel bilgilerini anonimleştirir.
        /// Aktör her zaman token'dan çözülür; istek gövdesinden kullanıcı ID'si kabul edilmez.
        /// </summary>
        /// <param name="command">Mevcut şifreyi taşıyan silme isteği</param>
        /// <returns>İşlem sonucu</returns>
        [HttpPost("delete-account")]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountCommand command)
        {
            // 1. Command'i mediator ile gönder (kimlik handler içinde token'dan çözülür)
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
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