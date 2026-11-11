using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Cities.Queries.GetAll;
using AppointMan.Application.Features.Districts.Queries.GetByCity;
using AppointMan.Application.Features.Localities.Queries.GetByDistrict;
using AppointMan.Application.Features.Neighborhoods.Queries.GetByLocality;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Adres hiyerarsisi ile ilgili tum listeleme islemlerini yonetir.
    /// Zincir: Il -> Ilce -> Locality -> Mahalle.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AddressController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AddressController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tum illeri getirir.
        /// </summary>
        /// <param name="onlyActive">Sadece aktif iller mi? (opsiyonel)</param>
        /// <returns>Il listesi</returns>
        [HttpGet("cities")]
        [ProducesResponseType(typeof(Result<List<CityDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<CityDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCities([FromQuery] bool? onlyActive = null)
        {
            // 1. Query'yi olustur
            var query = new GetCitiesQuery { OnlyActive = onlyActive };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Bir ilin ilcelerini getirir.
        /// </summary>
        /// <param name="cityId">Il ID'si (plaka kodu)</param>
        /// <param name="onlyActive">Sadece aktif ilceler mi? (opsiyonel)</param>
        /// <returns>Ilce listesi</returns>
        [HttpGet("districts/{cityId}")]
        [ProducesResponseType(typeof(Result<List<DistrictDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<DistrictDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDistricts(int cityId, [FromQuery] bool? onlyActive = null)
        {
            // 1. Query'yi olustur
            var query = new GetDistrictsByCityQuery
            {
                CityId = cityId,
                OnlyActive = onlyActive
            };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Bir ilcenin locality (semt) kayitlarini getirir.
        /// </summary>
        /// <param name="districtId">Ilce ID'si</param>
        /// <param name="onlyActive">Sadece aktif kayitlar mi? (opsiyonel)</param>
        /// <returns>Locality listesi</returns>
        [HttpGet("localities/{districtId}")]
        [ProducesResponseType(typeof(Result<List<LocalityDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<LocalityDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLocalities(int districtId, [FromQuery] bool? onlyActive = null)
        {
            // 1. Query'yi olustur
            var query = new GetLocalitiesByDistrictQuery
            {
                DistrictId = districtId,
                OnlyActive = onlyActive
            };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
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
            // 1. Query'yi olustur
            var query = new GetNeighborhoodsByLocalityQuery
            {
                LocalityId = localityId,
                OnlyActive = onlyActive
            };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}
