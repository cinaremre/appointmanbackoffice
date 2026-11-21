using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Reporting.Queries.GetBusinessDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Raporlama uclari (dokuman Modul 15.1: yonetici dashboard).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReportController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Isletmenin bir gunluk yonetici dashboard kartlarini getirir.
        /// </summary>
        /// <param name="businessId">Isletme ID'si</param>
        /// <param name="date">Raporlanacak gun (UTC; saat bilgisi yoksayilir)</param>
        /// <returns>Gunluk kartlar</returns>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(Result<BusinessDashboardDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<BusinessDashboardDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDashboard(Guid businessId, [FromQuery] DateTime? date = null)
        {
            var query = new GetBusinessDashboardQuery
            {
                BusinessId = businessId,
                Date = date ?? DateTime.UtcNow.Date
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}
