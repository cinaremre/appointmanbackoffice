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
    /// Satis ve iade islemlerini yonetir. Yetki kontrolleri handler
    /// katmaninda JWT kimligi ve isletme sahibi uzerinden yapilir;
    /// istek govdesindeki kimlik alanlari yetki kaynagi degildir.
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
        /// Yeni satis olusturur. Ilk surumde satis tek adimda tamamlanir.
        /// </summary>
        /// <param name="command">Satis bilgileri</param>
        /// <returns>Olusturulan satisin ID'si</returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateSaleCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Odemeye bagli ters (iade) kaydi olusturur. Negatif tutar yerine
        /// ayri bir iade odemesi acilir; nakit iadeler kasa hareketi uretir.
        /// </summary>
        /// <param name="command">Iade bilgileri</param>
        /// <returns>Olusturulan iade kaydinin ID'si</returns>
        [HttpPost("refund")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Refund([FromBody] RefundPaymentCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Bir UTC gununun satis/kasa ozetini getirir.
        /// </summary>
        /// <param name="businessId">Isletme ID'si</param>
        /// <param name="date">Ozet istenen gun (UTC)</param>
        /// <returns>Gunluk satis ozeti</returns>
        [HttpGet("daily-summary")]
        [ProducesResponseType(typeof(Result<DailySalesSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<DailySalesSummaryDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DailySummary([FromQuery] Guid businessId, [FromQuery] DateTime date)
        {
            // 1. Query'yi olustur
            var query = new GetDailySalesSummaryQuery { BusinessId = businessId, Date = date };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}
