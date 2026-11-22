using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Marketing.Commands.AttributeAppointment;
using Rezervise.Application.Features.Marketing.Commands.CreateCampaign;
using Rezervise.Application.Features.Marketing.Commands.RecordCampaignSpend;
using Rezervise.Application.Features.Marketing.Queries.GetCampaignRoi;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Pazarlama kampanyalari: olusturma, harcama girisi, randevu atfi ve
    /// ROI raporu. Tum uclar isletme sahibi (veya admin) icin yetkilidir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CampaignController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CampaignController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Yeni kampanya olusturur.
        /// </summary>
        /// <param name="command">Kampanya bilgileri</param>
        /// <returns>Kampanya ID'si</returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCampaignCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kampanya harcamasi kaydeder. Ilk surumde ayni tarih/tutar tekrar
        /// girilebilir; cift kayit engeli bilincli olarak yoktur.
        /// </summary>
        /// <param name="command">Harcama bilgileri</param>
        /// <returns>Harcama kaydi ID'si</returns>
        [HttpPost("spend")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RecordSpend([FromBody] RecordCampaignSpendCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Randevuyu kampanyaya atfeder. Ayni randevuya ikinci atif Conflict doner.
        /// </summary>
        /// <param name="command">Atif bilgileri</param>
        /// <returns>Atif kaydi ID'si</returns>
        [HttpPost("attribute")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Attribute([FromBody] AttributeAppointmentCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kampanyanin donem ozetini getirir: toplam harcama, atfedilen
        /// randevu ve gerceklesen randevu sayilari. Klasik ROAS satis
        /// entegrasyonu sonrasi hesaplanacak; aciklama RoasDesc alanindadir.
        /// </summary>
        /// <param name="campaignId">Kampanya ID'si</param>
        /// <param name="fromDate">Opsiyonel donem baslangici</param>
        /// <param name="toDate">Opsiyonel donem bitisi</param>
        /// <returns>Kampanya ROI ozeti</returns>
        [HttpGet("roi/{campaignId}")]
        [ProducesResponseType(typeof(Result<CampaignRoiDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<CampaignRoiDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoi(Guid campaignId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            // 1. Query'yi olustur
            var query = new GetCampaignRoiQuery { CampaignId = campaignId, FromDate = fromDate, ToDate = toDate };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}
