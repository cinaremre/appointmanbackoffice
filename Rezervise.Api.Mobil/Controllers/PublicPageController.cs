using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.PublicPages.Commands.PublishPublicPage;
using Rezervise.Application.Features.PublicPages.Queries.GetPublicPageBySlug;
using Rezervise.Application.Features.Tracking.Commands.RecordTrackingEvent;
using Rezervise.Application.Features.Tracking.Queries.GetTrackingLinkStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// İşletmenin kamuya açık rezervasyon sayfası ve anonim takip ölçümü.
    /// Yayınlama uçları işletme sahibine özeldir; by-slug ve tracking/event
    /// uçları anonimdir (bilgi sızdırmaz: hedef bulunamazsa sessiz başarı).
    /// QR kod görseli frontend tarafında üretilir; payload bu API'den
    /// dönen slug/kod ile kurulur.
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
        /// İşletmenin public sayfasını yayına alır. İşletmeye ait tek sayfa
        /// vardır: varsa günceller, yoksa oluşturur. Slug başka işletmede
        /// kullanılıyorsa Conflict döner.
        /// </summary>
        /// <param name="command">Yayın bilgileri (BusinessId, Slug).</param>
        /// <returns>Sayfa ID'si</returns>
        [HttpPost("publish")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Publish([FromBody] PublishPublicPageCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Slug ile public sayfa özetini getirir. ANONIM üçtür:
        /// yayında olmayan sayfa NotFound döner.
        /// </summary>
        /// <param name="slug">Sayfa slug'i</param>
        /// <returns>İşletme adı ve aktif hizmet listesi</returns>
        [HttpGet("by-slug/{slug}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<PublicPageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<PublicPageDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            // 1. Query'yi oluştur (yetki kontrolü yoktur)
            var query = new GetPublicPageBySlugQuery { Slug = slug };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Anonim takip olayı kaydeder. ANONIM üçtür; kod veya slug ile
        /// çağrılır. Hedef bulunamazsa sessiz başarı döner.
        /// </summary>
        /// <param name="command">Takip kodu/slug ve olay tipi</param>
        [HttpPost("tracking/event")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RecordTrackingEvent([FromBody] RecordTrackingEventCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// İşletmenin takip linklerinin ölçüm özetini getirir:
        /// toplam açılış, benzersiz oturum ve olay tipi kırılımı.
        /// </summary>
        /// <param name="businessId">İşletme ID'si</param>
        /// <returns>Link basına istatistik listesi</returns>
        [HttpGet("stats/{businessId}")]
        [ProducesResponseType(typeof(Result<List<TrackingLinkStatsDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<TrackingLinkStatsDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStats(Guid businessId)
        {
            // 1. Query'yi oluştur
            var query = new GetTrackingLinkStatsQuery { BusinessId = businessId };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}
