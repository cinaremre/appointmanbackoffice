using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.WorkingHours.Commands.AddWorkingHour;
using Rezervise.Application.Features.WorkingHours.Commands.RemoveWorkingHour;
using Rezervise.Application.Features.WorkingHours.Commands.SetClosed;
using Rezervise.Application.Features.WorkingHours.Commands.SetOpen;
using Rezervise.Application.Features.WorkingHours.Commands.UpdateWorkingHour;
using Rezervise.Application.Features.WorkingHours.Queries.GetByBusiness;
using Rezervise.Application.Features.WorkingHours.Queries.GetByDay;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// İşletme çalışma saatleri ile ilgili işlemleri yönetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorkingHourController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public WorkingHourController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Sahip ID'si her zaman JWT'den çözülür; gövde OwnerId değeri yetki kaynağı değildir.
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
        /// İşletmenin tüm çalışma saatlerini getirir.
        /// </summary>
        /// <param name="businessId">İşletme ID'si</param>
        /// <returns>Çalışma saati listesi</returns>
        [HttpGet("business/{businessId}")]
        [ProducesResponseType(typeof(Result<List<WorkingHourDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<WorkingHourDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByBusiness(Guid businessId)
        {
            // 1. Query'yi oluştur
            var query = new GetWorkingHoursByBusinessQuery { BusinessId = businessId };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Belirli bir günün çalışma saatini getirir.
        /// </summary>
        /// <param name="businessId">İşletme ID'si</param>
        /// <param name="day">Gün (0=Pazar, 1=Pazartesi, ...)</param>
        /// <returns>Çalışma saati detayı</returns>
        [HttpGet("business/{businessId}/day/{day}")]
        [ProducesResponseType(typeof(Result<WorkingHourDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<WorkingHourDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByDay(Guid businessId, DayOfWeek day)
        {
            // 1. Query'yi oluştur
            var query = new GetWorkingHourByDayQuery { BusinessId = businessId, Day = day };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Yeni çalışma saati ekler.
        /// </summary>
        /// <param name="command">Çalışma saati bilgileri</param>
        /// <returns>Oluşturulan çalışma saatinin ID'si</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Add([FromBody] AddWorkingHourCommand command)
        {
            // 1. OwnerId JWT'den gelir; gövde değerine güvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            // 2. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Çalışma saatini günceller.
        /// </summary>
        /// <param name="command">Güncelleme bilgileri</param>
        /// <returns>Güncellenen çalışma saatinin ID'si</returns>
        [HttpPut]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateWorkingHourCommand command)
        {
            // 1. OwnerId JWT'den gelir; gövde değerine güvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            // 2. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Çalışma saatini siler.
        /// </summary>
        /// <param name="id">Çalışma saati ID'si</param>
        /// <param name="command">Silme bilgileri</param>
        /// <returns>Silinen çalışma saatinin ID'si</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Remove(Guid id, [FromBody] RemoveWorkingHourCommand command)
        {
            // 1. ID ve sahip kimliği güvenilir kaynaklardan alınır.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.WorkingHourId = id;
            command.OwnerId = userId;

            // 2. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Günü kapalı olarak işaretler.
        /// </summary>
        /// <param name="id">Çalışma saati ID'si</param>
        /// <param name="command">Kapatma bilgileri</param>
        /// <returns>Güncellenen çalışma saatinin ID'si</returns>
        [HttpPost("{id}/close")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetClosed(Guid id, [FromBody] SetClosedCommand command)
        {
            // 1. ID ve sahip kimliği güvenilir kaynaklardan alınır.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.WorkingHourId = id;
            command.OwnerId = userId;

            // 2. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Günü açık olarak işaretler.
        /// </summary>
        /// <param name="id">Çalışma saati ID'si</param>
        /// <param name="command">Açma bilgileri</param>
        /// <returns>Güncellenen çalışma saatinin ID'si</returns>
        [HttpPost("{id}/open")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetOpen(Guid id, [FromBody] SetOpenCommand command)
        {
            // 1. ID ve sahip kimliği güvenilir kaynaklardan alınır.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.WorkingHourId = id;
            command.OwnerId = userId;

            // 2. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}