using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Notifications.Commands.DeleteNotification;
using AppointMan.Application.Features.Notifications.Commands.MarkAllAsRead;
using AppointMan.Application.Features.Notifications.Commands.MarkAsRead;
using AppointMan.Application.Features.Notifications.Queries.GetNotificationById;
using AppointMan.Application.Features.Notifications.Queries.GetNotificationsByUser;
using AppointMan.Application.Features.Notifications.Queries.GetUnreadNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Kullanici bildirimleri ile ilgili islemleri yonetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Hata kodunu dogru HTTP durum koduna esler. Yetki hatasi 404 olarak donulurse
        /// istemci kaynagin var olmadigini sanar; bu yuzden kod bazli esleme yapilir.
        /// </summary>
        /// <param name="result">Basarisiz sonuc.</param>
        /// <returns>Uygun HTTP durum kodu ile sarilmis sonuc.</returns>
        private IActionResult FromError<T>(Result<T> result)
        {
            return result.Error?.Code switch
            {
                "NOT_FOUND" => NotFound(result),
                "UNAUTHORIZED" => Unauthorized(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                _ => BadRequest(result)
            };
        }

        /// <summary>
        /// Kullanicinin tum bildirimlerini getirir.
        /// </summary>
        /// <param name="userId">Kullanici ID'si</param>
        /// <param name="pageNumber">Sayfa numarasi (varsayilan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayilan: 10)</param>
        /// <returns>Bildirim listesi</returns>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(Result<List<NotificationDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<NotificationDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByUser(
            Guid userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // 1. Query'yi olustur
            var query = new GetNotificationsByUserQuery
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
        /// Kullanicinin okunmamis bildirimlerini getirir.
        /// </summary>
        /// <param name="userId">Kullanici ID'si</param>
        /// <returns>Okunmamis bildirim listesi</returns>
        [HttpGet("user/{userId}/unread")]
        [ProducesResponseType(typeof(Result<List<NotificationDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<NotificationDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUnread(Guid userId)
        {
            // 1. Query'yi olustur
            var query = new GetUnreadNotificationsQuery { UserId = userId };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Bildirim detayini getirir.
        /// </summary>
        /// <param name="id">Bildirim ID'si</param>
        /// <returns>Bildirim detayi</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<NotificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<NotificationDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // 1. Query'yi olustur.
            //    Kullanici kimligi handler icinde token'dan cozulur; istekten alinmaz.
            var query = new GetNotificationByIdQuery { NotificationId = id };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return FromError(result);
        }

        /// <summary>
        /// Bildirimi okundu olarak isaretler.
        /// </summary>
        /// <param name="id">Bildirim ID'si</param>
        /// <param name="command">Isaretleme bilgileri</param>
        /// <returns>Isaretlenen bildirimin ID'si</returns>
        [HttpPost("{id}/mark-as-read")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkAsRead(Guid id, [FromBody] MarkAsReadCommand command)
        {
            // 1. Route'taki ID'yi command'e ata. Kullanici kimligi handler icinde token'dan cozulur.
            command.NotificationId = id;

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return FromError(result);
        }

        /// <summary>
        /// Kullanicinin tum bildirimlerini okundu olarak isaretler.
        /// </summary>
        /// <param name="userId">Kullanici ID'si</param>
        /// <param name="command">Isaretleme bilgileri</param>
        /// <returns>Isaretlenen bildirim sayisi</returns>
        [HttpPost("user/{userId}/mark-all-as-read")]
        [ProducesResponseType(typeof(Result<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkAllAsRead(Guid userId, [FromBody] MarkAllAsReadCommand command)
        {
            // 1. ID'yi command'e ata
            command.UserId = userId;

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Bildirimi siler.
        /// </summary>
        /// <param name="id">Bildirim ID'si</param>
        /// <param name="command">Silme bilgileri</param>
        /// <returns>Silinen bildirimin ID'si</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(Guid id, [FromBody] DeleteNotificationCommand command)
        {
            // 1. Route'taki ID'yi command'e ata. Kullanici kimligi handler icinde token'dan cozulur.
            command.NotificationId = id;

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return FromError(result);
        }
    }
}