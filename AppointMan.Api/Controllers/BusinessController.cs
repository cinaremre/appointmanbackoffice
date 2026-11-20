using System.Security.Claims;
using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Businesses.Commands.ActivateBusiness;
using AppointMan.Application.Features.Businesses.Commands.CreateBusiness;
using AppointMan.Application.Features.Businesses.Commands.DeactivateBusiness;
using AppointMan.Application.Features.Businesses.Commands.UpdateBusiness;
using AppointMan.Application.Features.Businesses.Queries.GetBusinessById;
using AppointMan.Application.Features.Businesses.Queries.GetBusinessesByOwner;
using AppointMan.Application.Features.Businesses.Queries.SearchBusinesses;
using AppointMan.Application.Features.Businesses.Queries.SearchBusinessesByService;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Isletme islemlerini yonetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BusinessController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public BusinessController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Sahip ID'si her zaman JWT'den cozulur; govde OwnerId degeri yetki kaynagi degildir.
        /// </summary>
        private IActionResult? ResolveCurrentUserId(out Guid userId)
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            {
                userId = Guid.Empty;
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Auth.InvalidUser")));
            }

            userId = _currentUser.UserId.Value;
            return null;
        }

        /// <summary>
        /// Yeni isletme olusturur.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateBusinessCommand command)
        {
            // OwnerId JWT'den gelir; govde degerine guvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Isletme bilgilerini gunceller.
        /// </summary>
        [HttpPut]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateBusinessCommand command)
        {
            // OwnerId JWT'den gelir; govde degerine guvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Isletmeyi aktif eder.
        /// </summary>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Activate(Guid id, [FromBody] ActivateBusinessCommand command)
        {
            // ID ve sahip kimligi guvenilir kaynaklardan alinir.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.BusinessId = id;
            command.OwnerId = userId;
            command.ActivatedBy = userId.ToString();
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Isletmeyi pasif eder.
        /// </summary>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Deactivate(Guid id, [FromBody] DeactivateBusinessCommand command)
        {
            // ID ve sahip kimligi guvenilir kaynaklardan alinir.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.BusinessId = id;
            command.OwnerId = userId;
            command.DeactivatedBy = userId.ToString();
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// ID'ye gore isletme getirir.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<BusinessDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<BusinessDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetBusinessByIdQuery { BusinessId = id };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Sahibine gore isletmeleri getirir.
        /// </summary>
        [HttpGet("owner/{ownerId}")]
        [ProducesResponseType(typeof(Result<List<BusinessDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<BusinessDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByOwner(
            Guid ownerId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetBusinessesByOwnerQuery
            {
                OwnerId = ownerId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Kategori, sehir ve ilceye gore isletme arar.
        /// </summary>
        /// <param name="categoryId">Isletme kategorisi (opsiyonel).</param>
        /// <param name="city">
        /// Sehir ADI (orn. "Ankara"). Sayisal ID verilmemisse bu ada gore
        /// City tablosundan ID cozulur.
        /// </param>
        /// <param name="cityId">
        /// Sehir ID'si (opsiyonel). Verilirse <paramref name="city"/> dikkate alinmaz.
        /// </param>
        /// <param name="districtId">Ilce ID'si (opsiyonel).</param>
        /// <param name="localityId">Locality ID'si (opsiyonel).</param>
        /// <param name="neighborhoodId">Mahalle ID'si (opsiyonel).</param>
        /// <param name="pageNumber">Sayfa numarasi (varsayilan: 1).</param>
        /// <param name="pageSize">Sayfa boyutu (varsayilan: 20).</param>
        /// <returns>Eslesen isletme listesi.</returns>
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<List<BusinessDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<BusinessDto>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
            [FromQuery] Guid? categoryId = null,
            [FromQuery] string? city = null,
            [FromQuery] int? cityId = null,
            [FromQuery] int? districtId = null,
            [FromQuery] int? localityId = null,
            [FromQuery] int? neighborhoodId = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            // Kullanici girisli ise ID'si sorguya aktarilir: konum kriteri bos
            // gelirse kullanicinin kayitli il/ilce bilgisi kullanilir.
            var query = new SearchBusinessesQuery
            {
                CategoryId = categoryId,
                City = city,
                CityId = cityId,
                DistrictId = districtId,
                LocalityId = localityId,
                NeighborhoodId = neighborhoodId,
                CurrentUserId = GetCurrentUserId(),
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Hizmet kategorisi veya hizmet adina gore isletme arar.
        /// Ornek: "Manikur" arayan kullanici, manikur hizmeti veren tum isletmeleri gorur.
        /// </summary>
        /// <param name="serviceCategoryId">Hizmet kategorisi (opsiyonel)</param>
        /// <param name="serviceSearchTerm">Hizmet adi (opsiyonel)</param>
        /// <param name="businessCategoryId">Isletme kategorisi (opsiyonel)</param>
        /// <param name="city">Sehir (zorunlu)</param>
        /// <param name="district">Ilce (opsiyonel)</param>
        /// <param name="pageNumber">Sayfa numarasi (varsayilan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayilan: 20)</param>
        /// <param name="searchTerm">
        /// Birlestirilmis arama terimi: isletme adi, hizmet adi veya sehir adindan biri
        /// eslesirse isletme listelenir (opsiyonel).
        /// </param>
        /// <returns>Isletme listesi</returns>
        [HttpGet("search-by-service")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<List<BusinessDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<BusinessDto>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchByService(
            [FromQuery] string? searchTerm = null,
            [FromQuery] Guid? serviceCategoryId = null,
            [FromQuery] string? serviceSearchTerm = null,
            [FromQuery] Guid? businessCategoryId = null,
            [FromQuery] int? cityId = null,
            [FromQuery] int? districtId = null,
            [FromQuery] int? localityId = null,
            [FromQuery] int? neighborhoodId = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            // Kullanici girisli ise ID'si sorguya aktarilir: konum kriteri bos
            // gelirse kullanicinin kayitli il/ilce bilgisi kullanilir.
            var query = new SearchBusinessesByServiceQuery
            {
                SearchTerm = searchTerm,
                ServiceCategoryId = serviceCategoryId,
                ServiceSearchTerm = serviceSearchTerm,
                BusinessCategoryId = businessCategoryId,
                CityId = cityId,
                DistrictId = districtId,
                LocalityId = localityId,
                NeighborhoodId = neighborhoodId,
                CurrentUserId = GetCurrentUserId(),
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Token icindeki kullanici ID'sini okur. Anonim istekte null doner.
        /// Arama sorgularinda konum kriteri bos geldiginde kullanicinin kayitli
        /// il/ilce bilgisini kullanabilmek icin gereklidir.
        /// </summary>
        private Guid? GetCurrentUserId()
        {
            var rawUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(rawUserId, out var userId))
                return userId;

            return null;
        }
    }
}