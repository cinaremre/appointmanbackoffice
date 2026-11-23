using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Reporting.Queries.GetBusinessDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Raporlama uçları (doküman Modül 15.1: yönetici dashboard).
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
        /// İşletmenin bir günlük yönetici dashboard kartlarını getirir.
        /// </summary>
        /// <param name="businessId">İşletme ID'si</param>
        /// <param name="date">Raporlanacak gün (UTC; saat bilgisi yoksayılır)</param>
        /// <returns>Günlük kartlar</returns>
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
