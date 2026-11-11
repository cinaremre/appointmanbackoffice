using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Employees.Commands.ActivateEmployee;
using AppointMan.Application.Features.Employees.Commands.CreateEmployee;
using AppointMan.Application.Features.Employees.Commands.DeactivateEmployee;
using AppointMan.Application.Features.Employees.Commands.UpdateEmployee;
using AppointMan.Application.Features.Employees.Queries.GetAvailableEmployees;
using AppointMan.Application.Features.Employees.Queries.GetEmployeeById;
using AppointMan.Application.Features.Employees.Queries.GetEmployeesByBusiness;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
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

        public EmployeeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Yeni calisan olusturur.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeCommand command)
        {
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
            command.EmployeeId = id;
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
            command.EmployeeId = id;
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
    }
}