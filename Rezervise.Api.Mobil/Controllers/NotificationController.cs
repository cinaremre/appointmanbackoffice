using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.DeviceTokens.Commands.RegisterDeviceToken;
using Rezervise.Application.Features.DeviceTokens.Commands.UnregisterDeviceToken;
using Rezervise.Application.Features.Notifications.Commands.DeleteNotification;
using Rezervise.Application.Features.Notifications.Commands.MarkAllAsRead;
using Rezervise.Application.Features.Notifications.Commands.MarkAsRead;
using Rezervise.Application.Features.Notifications.Queries.GetNotificationById;
using Rezervise.Application.Features.Notifications.Queries.GetNotificationsByUser;
using Rezervise.Application.Features.Notifications.Queries.GetUnreadNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Kullanıcı bildirimleri ile ilgili işlemleri yönetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public NotificationController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Route'taki kullanıcı ID'si ile token'daki kimliğin aynı olmasını doğrular.
        /// Uyuşmazsa başka bir kullanıcının bildirimlerine erişim engellenir (IDOR koruması).
        /// </summary>
        /// <param name="routeUserId">Route'tan gelen kullanıcı ID'si.</param>
        /// <returns>Doğrulama başarısızsa dönülecek sonuç; başarılıysa null.</returns>
        private IActionResult? EnsureOwnership(Guid routeUserId)
        {
            var currentUserId = _currentUser.UserId;

            if (currentUserId is null)
                return Unauthorized();

            if (routeUserId != currentUserId.Value)
                return StatusCode(StatusCodes.Status403Forbidden);

            return null;
        }

        /// <summary>
        /// Hata kodunu doğru HTTP durum koduna eşler. Yetki hatası 404 olarak dönülürse
        /// istemci kaynağın var olmadığını sanar; bu yüzden kod bazlı eşleme yapılır.
        /// </summary>
        /// <param name="result">Başarısız sonuç.</param>
        /// <returns>Uygun HTTP durum kodu ile sarılmış sonuç.</returns>
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
        /// Kullanıcının tüm bildirimlerini getirir.
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <param name="pageNumber">Sayfa numarası (varsayılan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayılan: 10)</param>
        /// <returns>Bildirim listesi</returns>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(Result<List<NotificationDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<NotificationDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByUser(
            Guid userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // Kimlik token'dan çözülür; route'taki ID yalnızca sahiplik doğrulaması için kullanılır.
            var ownership = EnsureOwnership(userId);
            if (ownership is not null)
                return ownership;

            // 1. Query'yi oluştur
            var query = new GetNotificationsByUserQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return FromError(result);
        }

        /// <summary>
        /// Kullanıcının okunmamış bildirimlerini getirir.
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <returns>Okunmamış bildirim listesi</returns>
        [HttpGet("user/{userId}/unread")]
        [ProducesResponseType(typeof(Result<List<NotificationDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<NotificationDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUnread(Guid userId)
        {
            // Kimlik token'dan çözülür; route'taki ID yalnızca sahiplik doğrulaması için kullanılır.
            var ownership = EnsureOwnership(userId);
            if (ownership is not null)
                return ownership;

            // 1. Query'yi oluştur
            var query = new GetUnreadNotificationsQuery();

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return FromError(result);
        }

        /// <summary>
        /// Bildirim detayını getirir.
        /// </summary>
        /// <param name="id">Bildirim ID'si</param>
        /// <returns>Bildirim detayı</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<NotificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<NotificationDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // 1. Query'yi oluştur.
            //    Kullanıcı kimliği handler içinde token'dan çözülür; istekten alınmaz.
            var query = new GetNotificationByIdQuery { NotificationId = id };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return FromError(result);
        }

        /// <summary>
        /// Bildirimi okundu olarak işaretler.
        /// </summary>
        /// <param name="id">Bildirim ID'si</param>
        /// <param name="command">İşaretleme bilgileri</param>
        /// <returns>İşaretlenen bildirimin ID'si</returns>
        [HttpPost("{id}/mark-as-read")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkAsRead(Guid id, [FromBody] MarkAsReadCommand command)
        {
            // 1. Route'taki ID'yi command'e ata. Kullanıcı kimliği handler içinde token'dan çözülür.
            command.NotificationId = id;

            // 2. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return FromError(result);
        }

        /// <summary>
        /// Kullanıcının tüm bildirimlerini okundu olarak işaretler.
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <param name="command">İşaretleme bilgileri</param>
        /// <returns>İşaretlenen bildirim sayısı</returns>
        [HttpPost("user/{userId}/mark-all-as-read")]
        [ProducesResponseType(typeof(Result<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkAllAsRead(Guid userId, [FromBody] MarkAllAsReadCommand command)
        {
            // Kimlik token'dan çözülür; route'taki ID yalnızca sahiplik doğrulaması için kullanılır.
            var ownership = EnsureOwnership(userId);
            if (ownership is not null)
                return ownership;

            // 1. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return FromError(result);
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
            // 1. Route'taki ID'yi command'e ata. Kullanıcı kimliği handler içinde token'dan çözülür.
            command.NotificationId = id;

            // 2. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return FromError(result);
        }

        // ===== Push bildirim cihaz kaydı =====

        /// <summary>
        /// Mobil cihazın FCM token'ini kaydeder. Kullanıcı yalnızca KENDI
        /// cihazını kaydedebilir (route'taki userId token kimliğiyle eşleşmelidir).
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <param name="command">Token ve platform bilgileri</param>
        /// <returns>Kayıt ID'si</returns>
        [HttpPost("user/{userId}/device-token")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RegisterDeviceToken(Guid userId, [FromBody] RegisterDeviceTokenCommand command)
        {
            // Kimlik token'dan çözülür; route'taki ID yalnızca sahiplik doğrulaması için kullanılır.
            var ownership = EnsureOwnership(userId);
            if (ownership is not null)
                return ownership;

            command.UserId = userId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return FromError(result);
        }

        /// <summary>
        /// Cihaz token'ini gönderime kapatır (çıkış / uygulama kaldırma).
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <param name="command">Kapatılacak token</param>
        /// <returns>İşlem kabul bilgisi</returns>
        [HttpDelete("user/{userId}/device-token")]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UnregisterDeviceToken(Guid userId, [FromBody] UnregisterDeviceTokenCommand command)
        {
            var ownership = EnsureOwnership(userId);
            if (ownership is not null)
                return ownership;

            command.UserId = userId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return FromError(result);
        }
    }
}