using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Appointments.Commands.CancelAppointment;
using Rezervise.Application.Features.Appointments.Commands.CompleteAppointment;
using Rezervise.Application.Features.Appointments.Commands.ConfirmAppointment;
using Rezervise.Application.Features.Appointments.Commands.CreateAppointment;
using Rezervise.Application.Features.Appointments.Commands.MarkAppointmentNoShow;
using Rezervise.Application.Features.Appointments.Commands.RescheduleAppointment;
using Rezervise.Application.Features.Appointments.Queries.GetAppointmentById;
using Rezervise.Application.Features.Appointments.Queries.GetAppointmentsByBusiness;
using Rezervise.Application.Features.Appointments.Queries.GetAppointmentsByUser;
using Rezervise.Application.Features.Appointments.Queries.GetAvailableSlots;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Randevu işlemlerini yönetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AppointmentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Yeni randevu oluşturur.
        /// </summary>
        /// <param name="command">Randevu bilgileri</param>
        /// <param name="idempotencyKey">
        /// Aynı isteğin ağ tekrarı veya çift tıklama ile mükerrer gönderilmesini
        /// engelleyen istemci anahtarı. Aynı anahtarla gelen ikinci istek
        /// yeni randevu üretmez, ilk oluşan randevunun ID'sını döner.
        /// </param>
        /// <returns>Oluşturulan randevunun ID'si</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create(
            [FromBody] CreateAppointmentCommand command,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
        {
            // JWT'den RequesterId'yi al
            var requesterId = GetRequesterId();
            if (requesterId == Guid.Empty)
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Auth.InvalidUser")));

            command.RequesterId = requesterId;
            command.IdempotencyKey = idempotencyKey;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Randevuyu iptal eder.
        /// </summary>
        [HttpPost("cancel")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cancel([FromBody] CancelAppointmentCommand command)
        {
            // JWT'den gelen kimlik gövdeye yazılan UserId'nin yerine geçer.
            var requesterId = GetRequesterId();
            if (requesterId == Guid.Empty)
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Auth.InvalidUser")));

            command.UserId = requesterId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Randevuyu "gelmedi" (no-show) olarak işaretler.
        /// </summary>
        /// <param name="command">İşaretlenecek randevu bilgisi</param>
        /// <returns>İşaretlenen randevunun ID'si</returns>
        [HttpPost("no-show")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkNoShow([FromBody] MarkAppointmentNoShowCommand command)
        {
            // JWT'den gelen kimlik gövdeye yazılan UserId'nin yerine geçer.
            var requesterId = GetRequesterId();
            if (requesterId == Guid.Empty)
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Auth.InvalidUser")));

            command.UserId = requesterId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Randevuyu onaylar.
        /// </summary>
        [HttpPost("confirm")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Confirm([FromBody] ConfirmAppointmentCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Randevuyu tamamlar.
        /// </summary>
        [HttpPost("complete")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Complete([FromBody] CompleteAppointmentCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Randevuyu yeniden planlar.
        /// </summary>
        [HttpPost("reschedule")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Reschedule([FromBody] RescheduleAppointmentCommand command)
        {
            // JWT'den gelen kimlik gövdeye yazılan UserId'nin yerine geçer.
            var requesterId = GetRequesterId();
            if (requesterId == Guid.Empty)
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Auth.InvalidUser")));

            command.UserId = requesterId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// ID'ye göre randevu getirir.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<AppointmentDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAppointmentByIdQuery { AppointmentId = id };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// İşletmeye göre randevuları getirir.
        /// </summary>
        [HttpGet("business/{businessId}")]
        [ProducesResponseType(typeof(Result<List<AppointmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<AppointmentDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByBusiness(
            Guid businessId,
            [FromQuery] Guid? employeeId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetAppointmentsByBusinessQuery
            {
                BusinessId = businessId,
                EmployeeId = employeeId,
                StartDate = startDate,
                EndDate = endDate,
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Kullanıcıya göre randevuları getirir.
        /// </summary>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(Result<List<AppointmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<AppointmentDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByUser(
            Guid userId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetAppointmentsByUserQuery
            {
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate,
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Belirli bir çalışanın belirli bir hizmet için müsait saatlerini getirir.
        /// </summary>
        [HttpGet("available-slots")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<List<AvailableSlotDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<AvailableSlotDto>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableSlots(
            [FromQuery] Guid businessId,
            [FromQuery] Guid employeeId,
            [FromQuery] Guid serviceId,
            [FromQuery] DateTime date)
        {
            var query = new GetAvailableSlotsQuery
            {
                BusinessId = businessId,
                EmployeeId = employeeId,
                ServiceId = serviceId,
                Date = date
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        // ===== HELPER =====

        private Guid GetRequesterId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Guid.Empty;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}