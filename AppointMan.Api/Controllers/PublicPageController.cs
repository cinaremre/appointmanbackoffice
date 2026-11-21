using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.PublicPages.Commands.PublishPublicPage;
using AppointMan.Application.Features.PublicPages.Queries.GetPublicPageBySlug;
using AppointMan.Application.Features.Tracking.Commands.RecordTrackingEvent;
using AppointMan.Application.Features.Tracking.Queries.GetTrackingLinkStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Isletmenin kamuya acik rezervasyon sayfasi ve anonim takip olcumu.
    /// Yayinlama uclari isletme sahibine ozeldir; by-slug ve tracking/event
    /// uclari anonimdir (bilgi sizdirmaz: hedef bulunamazsa sessiz basari).
    /// QR kod gorseli frontend tarafinda uretilir; payload bu API'den
    /// donen slug/kod ile kurulur.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PublicPageController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PublicPageController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Isletmenin public sayfasini yayina alir. Isletmeye ait tek sayfa
        /// vardir: varsa gunceller, yoksa olusturur. Slug baska isletmede
        /// kullaniliyorsa Conflict doner.
        /// </summary>
        /// <param name="command">Yayin bilgileri (BusinessId, Slug).</param>
        /// <returns>Sayfa ID'si</returns>
        [HttpPost("publish")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Publish([FromBody] PublishPublicPageCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Slug ile public sayfa ozetini getirir. ANONIM uctur:
        /// yayinda olmayan sayfa NotFound doner.
        /// </summary>
        /// <param name="slug">Sayfa slug'i</param>
        /// <returns>Isletme adi ve aktif hizmet listesi</returns>
        [HttpGet("by-slug/{slug}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<PublicPageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<PublicPageDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            // 1. Query'yi olustur (yetki kontrolu yoktur)
            var query = new GetPublicPageBySlugQuery { Slug = slug };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Anonim takip olayi kaydeder. ANONIM uctur; kod veya slug ile
        /// cagrilir. Hedef bulunamazsa sessiz basari doner.
        /// </summary>
        /// <param name="command">Takip kodu/slug ve olay tipi</param>
        [HttpPost("tracking/event")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RecordTrackingEvent([FromBody] RecordTrackingEventCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Isletmenin takip linklerinin olcum ozetini getirir:
        /// toplam acilis, benzersiz oturum ve olay tipi kirilimi.
        /// </summary>
        /// <param name="businessId">Isletme ID'si</param>
        /// <returns>Link basina istatistik listesi</returns>
        [HttpGet("stats/{businessId}")]
        [ProducesResponseType(typeof(Result<List<TrackingLinkStatsDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<TrackingLinkStatsDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStats(Guid businessId)
        {
            // 1. Query'yi olustur
            var query = new GetTrackingLinkStatsQuery { BusinessId = businessId };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}
