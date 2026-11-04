using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.WorkingHours.Commands.AddWorkingHour;
using AppointMan.Application.Features.WorkingHours.Commands.RemoveWorkingHour;
using AppointMan.Application.Features.WorkingHours.Commands.SetClosed;
using AppointMan.Application.Features.WorkingHours.Commands.SetOpen;
using AppointMan.Application.Features.WorkingHours.Commands.UpdateWorkingHour;
using AppointMan.Application.Features.WorkingHours.Queries.GetByBusiness;
using AppointMan.Application.Features.WorkingHours.Queries.GetByDay;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Isletme calisma saatleri ile ilgili islemleri yonetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorkingHourController : ControllerBase
    {
        private readonly IMediator _mediator;

        public WorkingHourController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Isletmenin tum calisma saatlerini getirir.
        /// </summary>
        /// <param name="businessId">Isletme ID'si</param>
        /// <returns>Calisma saati listesi</returns>
        [HttpGet("business/{businessId}")]
        [ProducesResponseType(typeof(Result<List<WorkingHourDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<WorkingHourDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByBusiness(Guid businessId)
        {
            // 1. Query'yi olustur
            var query = new GetWorkingHoursByBusinessQuery { BusinessId = businessId };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Belirli bir gunun calisma saatini getirir.
        /// </summary>
        /// <param name="businessId">Isletme ID'si</param>
        /// <param name="day">Gun (0=Pazar, 1=Pazartesi, ...)</param>
        /// <returns>Calisma saati detayi</returns>
        [HttpGet("business/{businessId}/day/{day}")]
        [ProducesResponseType(typeof(Result<WorkingHourDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<WorkingHourDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByDay(Guid businessId, DayOfWeek day)
        {
            // 1. Query'yi olustur
            var query = new GetWorkingHourByDayQuery { BusinessId = businessId, Day = day };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Yeni calisma saati ekler.
        /// </summary>
        /// <param name="command">Calisma saati bilgileri</param>
        /// <returns>Olusturulan calisma saatinin ID'si</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Add([FromBody] AddWorkingHourCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Calisma saatini gunceller.
        /// </summary>
        /// <param name="command">Guncelleme bilgileri</param>
        /// <returns>Guncellenen calisma saatinin ID'si</returns>
        [HttpPut]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateWorkingHourCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Calisma saatini siler.
        /// </summary>
        /// <param name="id">Calisma saati ID'si</param>
        /// <param name="command">Silme bilgileri</param>
        /// <returns>Silinen calisma saatinin ID'si</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Remove(Guid id, [FromBody] RemoveWorkingHourCommand command)
        {
            // 1. ID'yi command'e ata
            command.WorkingHourId = id;

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Gunu kapali olarak isaretler.
        /// </summary>
        /// <param name="id">Calisma saati ID'si</param>
        /// <param name="command">Kapatma bilgileri</param>
        /// <returns>Guncellenen calisma saatinin ID'si</returns>
        [HttpPost("{id}/close")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetClosed(Guid id, [FromBody] SetClosedCommand command)
        {
            // 1. ID'yi command'e ata
            command.WorkingHourId = id;

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Gunu acik olarak isaretler.
        /// </summary>
        /// <param name="id">Calisma saati ID'si</param>
        /// <param name="command">Acma bilgileri</param>
        /// <returns>Guncellenen calisma saatinin ID'si</returns>
        [HttpPost("{id}/open")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetOpen(Guid id, [FromBody] SetOpenCommand command)
        {
            // 1. ID'yi command'e ata
            command.WorkingHourId = id;

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}