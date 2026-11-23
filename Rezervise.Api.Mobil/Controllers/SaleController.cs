using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Sales.Commands.CreateSale;
using Rezervise.Application.Features.Sales.Commands.RefundPayment;
using Rezervise.Application.Features.Sales.Queries.GetDailySalesSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Satış ve iade işlemlerini yönetir. Yetki kontrolleri handler
    /// katmanında JWT kimliği ve işletme sahibi üzerinden yapılır;
    /// istek gövdesindeki kimlik alanları yetki kaynağı değildir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SaleController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SaleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Yeni satış oluşturur. İlk sürümde satış tek adımda tamamlanır.
        /// </summary>
        /// <param name="command">Satış bilgileri</param>
        /// <returns>Oluşturulan satışın ID'si</returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateSaleCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Ödemeye bağlı ters (iade) kaydı oluşturur. Negatif tutar yerine
        /// ayrı bir iade ödemesi açılır; nakit iadeler kasa hareketi üretir.
        /// </summary>
        /// <param name="command">İade bilgileri</param>
        /// <returns>Oluşturulan iade kaydının ID'si</returns>
        [HttpPost("refund")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Refund([FromBody] RefundPaymentCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Bir UTC gününün satış/kasa özetini getirir.
        /// </summary>
        /// <param name="businessId">İşletme ID'si</param>
        /// <param name="date">Özet istenen gün (UTC)</param>
        /// <returns>Günlük satış özeti</returns>
        [HttpGet("daily-summary")]
        [ProducesResponseType(typeof(Result<DailySalesSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<DailySalesSummaryDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DailySummary([FromQuery] Guid businessId, [FromQuery] DateTime date)
        {
            // 1. Query'yi oluştur
            var query = new GetDailySalesSummaryQuery { BusinessId = businessId, Date = date };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}
