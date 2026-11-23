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
    /// Pazarlama kampanyaları: oluşturma, harcama girişi, randevu atfi ve
    /// ROI raporu. Tüm uçlar işletme sahibi (veya admin) için yetkilidir.
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
        /// Yeni kampanya oluşturur.
        /// </summary>
        /// <param name="command">Kampanya bilgileri</param>
        /// <returns>Kampanya ID'si</returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCampaignCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kampanya harcaması kaydeder. İlk sürümde aynı tarih/tutar tekrar
        /// girilebilir; çift kayıt engeli bilinçli olarak yoktur.
        /// </summary>
        /// <param name="command">Harcama bilgileri</param>
        /// <returns>Harcama kaydı ID'si</returns>
        [HttpPost("spend")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RecordSpend([FromBody] RecordCampaignSpendCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Randevuyu kampanyaya atfeder. Aynı randevuya ikinci atıf Conflict döner.
        /// </summary>
        /// <param name="command">Atıf bilgileri</param>
        /// <returns>Atıf kaydı ID'si</returns>
        [HttpPost("attribute")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Attribute([FromBody] AttributeAppointmentCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kampanyanın dönem özetini getirir: toplam harcama, atfedilen
        /// randevu ve gerçekleşen randevu sayıları. Klasik ROAS satış
        /// entegrasyonu sonrası hesaplanacak; açıklama RoasDesc alanındadır.
        /// </summary>
        /// <param name="campaignId">Kampanya ID'si</param>
        /// <param name="fromDate">Opsiyonel dönem başlangıcı</param>
        /// <param name="toDate">Opsiyonel dönem bitişi</param>
        /// <returns>Kampanya ROI özeti</returns>
        [HttpGet("roi/{campaignId}")]
        [ProducesResponseType(typeof(Result<CampaignRoiDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<CampaignRoiDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoi(Guid campaignId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            // 1. Query'yi oluştur
            var query = new GetCampaignRoiQuery { CampaignId = campaignId, FromDate = fromDate, ToDate = toDate };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}
