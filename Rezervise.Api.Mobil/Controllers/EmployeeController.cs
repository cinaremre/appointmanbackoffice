using System.Security.Claims;
using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Employees.Commands.ActivateEmployee;
using Rezervise.Application.Features.Employees.Commands.ApproveTimeOff;
using Rezervise.Application.Features.Employees.Commands.CreateEmployee;
using Rezervise.Application.Features.Employees.Commands.DeactivateEmployee;
using Rezervise.Application.Features.Employees.Commands.RejectTimeOff;
using Rezervise.Application.Features.Employees.Commands.UpdateEmployee;
using Rezervise.Application.Features.Employees.Queries.GetAvailableEmployees;
using Rezervise.Application.Features.Employees.Queries.GetEmployeeById;
using Rezervise.Application.Features.Employees.Queries.GetEmployeesByBusiness;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Isletme calisanlari ile ilgili islemleri yonetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public EmployeeController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Sahip ID'si her zaman JWT'den cozulur; govde OwnerId degeri yetki kaynagi degildir.
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
        /// Yeni calisan olusturur.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeCommand command)
        {
            // OwnerId JWT'den gelir; govde degerine guvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Calisan bilgilerini gunceller.
        /// </summary>
        [HttpPut]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateEmployeeCommand command)
        {
            // OwnerId JWT'den gelir; govde degerine guvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Calisani aktif eder.
        /// </summary>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Activate(Guid id, [FromBody] ActivateEmployeeCommand command)
        {
            // ID ve sahip kimligi guvenilir kaynaklardan alinir.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.EmployeeId = id;
            command.OwnerId = userId;
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Calisani pasif eder.
        /// </summary>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Deactivate(Guid id, [FromBody] DeactivateEmployeeCommand command)
        {
            // ID ve sahip kimligi guvenilir kaynaklardan alinir.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.EmployeeId = id;
            command.OwnerId = userId;
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Calisan iznini onaylar.
        /// </summary>
        [HttpPost("{timeOffId}/approve")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ApproveTimeOff(
            Guid timeOffId,
            [FromBody] ApproveTimeOffCommand command)
        {
            // JWT'den gelen kimlik govdeye yazilan RequesterId'nin yerine gecer.
            var requesterId = GetRequesterId();
            if (requesterId == Guid.Empty)
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Auth.InvalidUser")));

            command.TimeOffId = timeOffId;
            command.RequesterId = requesterId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Calisan iznini reddeder.
        /// </summary>
        [HttpPost("{timeOffId}/reject")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RejectTimeOff(
            Guid timeOffId,
            [FromBody] RejectTimeOffCommand command)
        {
            // JWT'den gelen kimlik govdeye yazilan RequesterId'nin yerine gecer.
            var requesterId = GetRequesterId();
            if (requesterId == Guid.Empty)
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Auth.InvalidUser")));

            command.TimeOffId = timeOffId;
            command.RequesterId = requesterId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// ID'ye gore calisan getirir.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<EmployeeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<EmployeeDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetEmployeeByIdQuery { EmployeeId = id };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Isletmeye gore calisanlari getirir.
        /// </summary>
        [HttpGet("business/{businessId}")]
        [ProducesResponseType(typeof(Result<List<EmployeeDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<EmployeeDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByBusiness(
            Guid businessId,
            [FromQuery] bool? isActive = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetEmployeesByBusinessQuery
            {
                BusinessId = businessId,
                IsActive = isActive,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Musait calisanlari getirir (hizmet ve tarih filtreli).
        /// </summary>
        [HttpGet("business/{businessId}/available")]
        [ProducesResponseType(typeof(Result<List<EmployeeDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<EmployeeDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAvailable(
            Guid businessId,
            [FromQuery] Guid? serviceId = null,
            [FromQuery] DateTime? appointmentDate = null)
        {
            var query = new GetAvailableEmployeesQuery
            {
                BusinessId = businessId,
                ServiceId = serviceId,
                AppointmentDate = appointmentDate
            };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        // ===== HELPER =====

        /// <summary>
        /// AppointmentsController ile ayni sekilde JWT claim'lerinden istek sahibini okur.
        /// </summary>
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