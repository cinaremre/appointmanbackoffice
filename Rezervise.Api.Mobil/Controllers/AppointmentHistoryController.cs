using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.AppointmentHistories.Queries.GetByAppointment;
using Rezervise.Application.Features.AppointmentHistories.Queries.GetByBusiness;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Randevu gecmisi ile ilgili islemleri yonetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AppointmentHistoryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AppointmentHistoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Belirli bir randevunun tum durum degisiklik gecmisini getirir.
        /// </summary>
        /// <param name="appointmentId">Gecmisi getirilecek randevunun ID'si</param>
        /// <returns>Randevu gecmis kayitlarinin listesi</returns>
        [HttpGet("appointment/{appointmentId}")]
        [ProducesResponseType(typeof(Result<List<AppointmentHistoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<AppointmentHistoryDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByAppointment(Guid appointmentId)
        {
            // 1. Query'yi olustur ve mediator ile gonder
            var query = new GetAppointmentHistoryByAppointmentQuery { AppointmentId = appointmentId };
            var result = await _mediator.Send(query);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Belirli bir isletmenin tum randevularinin durum degisiklik gecmisini getirir.
        /// </summary>
        /// <param name="businessId">Isletmenin ID'si</param>
        /// <param name="startDate">Baslangic tarihi (opsiyonel)</param>
        /// <param name="endDate">Bitis tarihi (opsiyonel)</param>
        /// <param name="pageNumber">Sayfa numarasi (varsayilan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayilan: 10)</param>
        /// <returns>Randevu gecmis kayitlarinin sayfalanmis listesi</returns>
        [HttpGet("business/{businessId}")]
        [Authorize(Roles = "BusinessOwner,Admin,SuperAdmin")]
        [ProducesResponseType(typeof(Result<List<AppointmentHistoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<AppointmentHistoryDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByBusiness(
            Guid businessId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // 1. Query'yi olustur
            var query = new GetAppointmentHistoryByBusinessQuery
            {
                BusinessId = businessId,
                StartDate = startDate,
                EndDate = endDate,
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
    }
}