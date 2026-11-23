using System.Security.Claims;
using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Businesses.Commands.ActivateBusiness;
using Rezervise.Application.Features.Businesses.Commands.CreateBusiness;
using Rezervise.Application.Features.Businesses.Commands.DeactivateBusiness;
using Rezervise.Application.Features.Businesses.Commands.UpdateBusiness;
using Rezervise.Application.Features.Businesses.Queries.GetBusinessById;
using Rezervise.Application.Features.Businesses.Queries.GetBusinessesByOwner;
using Rezervise.Application.Features.Businesses.Queries.SearchBusinesses;
using Rezervise.Application.Features.Businesses.Queries.SearchBusinessesByService;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// İşletme işlemlerini yönetir.
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
        /// Sahip ID'si her zaman JWT'den çözülür; gövde OwnerId değeri yetki kaynağı değildir.
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
        /// Yeni işletme oluşturur.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateBusinessCommand command)
        {
            // OwnerId JWT'den gelir; gövde değerine güvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// İşletme bilgilerini günceller.
        /// </summary>
        [HttpPut]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateBusinessCommand command)
        {
            // OwnerId JWT'den gelir; gövde değerine güvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// İşletmeyi aktif eder.
        /// </summary>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Activate(Guid id, [FromBody] ActivateBusinessCommand command)
        {
            // ID ve sahip kimliği güvenilir kaynaklardan alınır.
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
        /// İşletmeyi pasif eder.
        /// </summary>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Deactivate(Guid id, [FromBody] DeactivateBusinessCommand command)
        {
            // ID ve sahip kimliği güvenilir kaynaklardan alınır.
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
        /// ID'ye göre işletme getirir.
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
        /// Sahibine göre işletmeleri getirir.
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
        /// Kategori, şehir ve ilçeye göre işletme arar.
        /// </summary>
        /// <param name="categoryId">İşletme kategorisi (opsiyonel).</param>
        /// <param name="city">
        /// Şehir ADI (orn. "Ankara"). Sayısal ID verilmemişse bu ada göre
        /// City tablosundan ID çözülür.
        /// </param>
        /// <param name="cityId">
        /// Şehir ID'si (opsiyonel). Verilirse <paramref name="city"/> dikkate alınmaz.
        /// </param>
        /// <param name="districtId">İlçe ID'si (opsiyonel).</param>
        /// <param name="localityId">Locality ID'si (opsiyonel).</param>
        /// <param name="neighborhoodId">Mahalle ID'si (opsiyonel).</param>
        /// <param name="pageNumber">Sayfa numarası (varsayılan: 1).</param>
        /// <param name="pageSize">Sayfa boyutu (varsayılan: 20).</param>
        /// <returns>Eşleşen işletme listesi.</returns>
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
            // Kullanıcı girişli ise ID'si sorguya aktarılır: konum kriteri boş
            // gelirse kullanıcının kayıtlı il/ilçe bilgisi kullanılır.
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
        /// Hizmet kategorisi veya hizmet adına göre işletme arar.
        /// Örnek: "Manikür" arayan kullanıcı, manikür hizmeti veren tüm işletmeleri görür.
        /// </summary>
        /// <param name="serviceCategoryId">Hizmet kategorisi (opsiyonel)</param>
        /// <param name="serviceSearchTerm">Hizmet adı (opsiyonel)</param>
        /// <param name="businessCategoryId">İşletme kategorisi (opsiyonel)</param>
        /// <param name="city">Şehir (zorunlu)</param>
        /// <param name="district">İlçe (opsiyonel)</param>
        /// <param name="pageNumber">Sayfa numarası (varsayılan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayılan: 20)</param>
        /// <param name="searchTerm">
        /// Birleştirilmiş arama terimi: işletme adı, hizmet adı veya şehir adından biri
        /// eşleşirse işletme listelenir (opsiyonel).
        /// </param>
        /// <returns>İşletme listesi</returns>
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
            // Kullanıcı girişli ise ID'si sorguya aktarılır: konum kriteri boş
            // gelirse kullanıcının kayıtlı il/ilçe bilgisi kullanılır.
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
        /// Token içindeki kullanıcı ID'sını okur. Anonim istekte null döner.
        /// Arama sorgularında konum kriteri boş geldiğinde kullanıcının kayıtlı
        /// il/ilçe bilgisini kullanabilmek için gereklidir.
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