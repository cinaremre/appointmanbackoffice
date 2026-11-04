using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Appointments.Commands.CancelAppointment;
using AppointMan.Application.Features.Appointments.Commands.CompleteAppointment;
using AppointMan.Application.Features.Appointments.Commands.ConfirmAppointment;
using AppointMan.Application.Features.Appointments.Commands.CreateAppointment;
using AppointMan.Application.Features.Appointments.Commands.RescheduleAppointment;
using AppointMan.Application.Features.Appointments.Queries.GetAppointmentById;
using AppointMan.Application.Features.Appointments.Queries.GetAppointmentsByBusiness;
using AppointMan.Application.Features.Appointments.Queries.GetAppointmentsByUser;
using AppointMan.Application.Features.Appointments.Queries.GetAvailableSlots;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Randevu islemlerini yonetir.
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
        /// Yeni randevu olusturur.
        /// </summary>
        /// <param name="command">Randevu bilgileri</param>
        /// <returns>Olusturulan randevunun ID'si</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Randevuyu iptal eder.
        /// </summary>
        /// <param name="command">Iptal bilgileri</param>
        /// <returns>Iptal edilen randevunun ID'si</returns>
        [HttpPost("cancel")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cancel([FromBody] CancelAppointmentCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Randevuyu onaylar.
        /// </summary>
        /// <param name="command">Onay bilgileri</param>
        /// <returns>Onaylanan randevunun ID'si</returns>
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
        /// <param name="command">Tamamlama bilgileri</param>
        /// <returns>Tamamlanan randevunun ID'si</returns>
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
        /// <param name="command">Yeniden planlama bilgileri</param>
        /// <returns>Yeniden planlanan randevunun ID'si</returns>
        [HttpPost("reschedule")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Reschedule([FromBody] RescheduleAppointmentCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// ID'ye gore randevu getirir.
        /// </summary>
        /// <param name="id">Randevu ID'si</param>
        /// <returns>Randevu detaylari</returns>
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
        /// Isletmeye gore randevulari getirir.
        /// </summary>
        /// <param name="businessId">Isletme ID'si</param>
        /// <param name="employeeId">Calisan ID'si (opsiyonel)</param>
        /// <param name="startDate">Baslangic tarihi (opsiyonel)</param>
        /// <param name="endDate">Bitis tarihi (opsiyonel)</param>
        /// <param name="status">Randevu durumu (opsiyonel)</param>
        /// <param name="pageNumber">Sayfa numarasi (varsayilan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayilan: 10)</param>
        /// <returns>Randevu listesi</returns>
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
        /// Kullaniciya gore randevulari getirir.
        /// </summary>
        /// <param name="userId">Kullanici ID'si</param>
        /// <param name="startDate">Baslangic tarihi (opsiyonel)</param>
        /// <param name="endDate">Bitis tarihi (opsiyonel)</param>
        /// <param name="status">Randevu durumu (opsiyonel)</param>
        /// <param name="pageNumber">Sayfa numarasi (varsayilan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayilan: 10)</param>
        /// <returns>Randevu listesi</returns>
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
        /// Belirli bir calisanin belirli bir hizmet icin musait saatlerini getirir.
        /// </summary>
        /// <param name="businessId">Isletme ID'si</param>
        /// <param name="employeeId">Calisan ID'si</param>
        /// <param name="serviceId">Hizmet ID'si</param>
        /// <param name="date">Tarih (yyyy-MM-dd)</param>
        /// <returns>Musait slotlarin listesi</returns>
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
    }
}