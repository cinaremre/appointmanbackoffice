using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Cities.Queries.GetAll;
using Rezervise.Application.Features.Districts.Queries.GetByCity;
using Rezervise.Application.Features.Localities.Queries.GetByDistrict;
using Rezervise.Application.Features.Neighborhoods.Queries.GetByDistrict;
using Rezervise.Application.Features.Neighborhoods.Queries.GetByLocality;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Adres hiyerarşisi ile ilgili tüm listeleme işlemlerini yönetir.
    /// Zincir: İl -> İlçe -> Locality -> Mahalle.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    // Adres hiyerarşisi kimliği doğrulanmış kullanıcılara açıktır; anonim erişim kapalıdır.
    [Authorize]
    public class AddressController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AddressController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tüm illeri getirir.
        /// </summary>
        /// <param name="onlyActive">Sadece aktif iller mi? (opsiyonel)</param>
        /// <returns>İl listesi</returns>
        [HttpGet("cities")]
        [ProducesResponseType(typeof(Result<List<CityDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<CityDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCities([FromQuery] bool? onlyActive = null)
        {
            // 1. Query'yi oluştur
            var query = new GetCitiesQuery { OnlyActive = onlyActive };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Bir ilin ilçelerini getirir.
        /// </summary>
        /// <param name="cityId">İl ID'si (plaka kodu)</param>
        /// <param name="onlyActive">Sadece aktif ilçeler mi? (opsiyonel)</param>
        /// <returns>İlçe listesi</returns>
        [HttpGet("districts/{cityId}")]
        [ProducesResponseType(typeof(Result<List<DistrictDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<DistrictDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDistricts(int cityId, [FromQuery] bool? onlyActive = null)
        {
            // 1. Query'yi oluştur
            var query = new GetDistrictsByCityQuery
            {
                CityId = cityId,
                OnlyActive = onlyActive
            };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Bir ilçenin locality (semt) kayıtlarını getirir.
        /// </summary>
        /// <param name="districtId">İlçe ID'si</param>
        /// <param name="onlyActive">Sadece aktif kayıtlar mi? (opsiyonel)</param>
        /// <returns>Locality listesi</returns>
        [HttpGet("localities/{districtId}")]
        [ProducesResponseType(typeof(Result<List<LocalityDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<LocalityDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLocalities(int districtId, [FromQuery] bool? onlyActive = null)
        {
            // 1. Query'yi oluştur
            var query = new GetLocalitiesByDistrictQuery
            {
                DistrictId = districtId,
                OnlyActive = onlyActive
            };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Bir ilçenin tüm mahallelerini (semt ayrımı olmadan) getirir.
        /// Mobil adres seçiminde ilçe seçildikten sonra tek çağrıyla mahalle
        /// listesi alınır; semti (localityId) boş olan mahalleler de döner.
        /// </summary>
        /// <param name="districtId">İlçe ID'si</param>
        /// <param name="onlyActive">Sadece aktif mahalleler mi? (opsiyonel)</param>
        /// <returns>Mahalle listesi</returns>
        [HttpGet("districts/{districtId}/neighborhoods")]
        [ProducesResponseType(typeof(Result<List<NeighborhoodDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<NeighborhoodDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetNeighborhoodsByDistrict(
            int districtId,
            [FromQuery] bool? onlyActive = null)
        {
            var query = new GetNeighborhoodsByDistrictQuery
            {
                DistrictId = districtId,
                OnlyActive = onlyActive
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Bir locality'nin mahallelerini getirir.
        /// </summary>
        /// <param name="localityId">Locality ID'si</param>
        /// <param name="onlyActive">Sadece aktif mahalleler mi? (opsiyonel)</param>
        /// <returns>Mahalle listesi</returns>
        [HttpGet("neighborhoods/{localityId}")]
        [ProducesResponseType(typeof(Result<List<NeighborhoodDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<NeighborhoodDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetNeighborhoods(int localityId, [FromQuery] bool? onlyActive = null)
        {
            // 1. Query'yi oluştur
            var query = new GetNeighborhoodsByLocalityQuery
            {
                LocalityId = localityId,
                OnlyActive = onlyActive
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
