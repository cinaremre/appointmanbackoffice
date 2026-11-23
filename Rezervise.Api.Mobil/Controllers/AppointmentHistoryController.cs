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
    /// Randevu geçmişi ile ilgili işlemleri yönetir.
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
        /// Belirli bir randevunun tüm durum değişiklik geçmişini getirir.
        /// </summary>
        /// <param name="appointmentId">Geçmişi getirilecek randevunun ID'si</param>
        /// <returns>Randevu geçmiş kayıtlarının listesi</returns>
        [HttpGet("appointment/{appointmentId}")]
        [ProducesResponseType(typeof(Result<List<AppointmentHistoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<AppointmentHistoryDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByAppointment(Guid appointmentId)
        {
            // 1. Query'yi oluştur ve mediator ile gönder
            var query = new GetAppointmentHistoryByAppointmentQuery { AppointmentId = appointmentId };
            var result = await _mediator.Send(query);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Belirli bir işletmenin tüm randevularının durum değişiklik geçmişini getirir.
        /// </summary>
        /// <param name="businessId">İşletmenin ID'si</param>
        /// <param name="startDate">Başlangıç tarihi (opsiyonel)</param>
        /// <param name="endDate">Bitiş tarihi (opsiyonel)</param>
        /// <param name="pageNumber">Sayfa numarası (varsayılan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayılan: 10)</param>
        /// <returns>Randevu geçmiş kayıtlarının sayfalanmış listesi</returns>
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
            // 1. Query'yi oluştur
            var query = new GetAppointmentHistoryByBusinessQuery
            {
                BusinessId = businessId,
                StartDate = startDate,
                EndDate = endDate,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}