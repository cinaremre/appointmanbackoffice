using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Services.Commands.ActivateService;
using Rezervise.Application.Features.Services.Commands.CreateService;
using Rezervise.Application.Features.Services.Commands.DeactivateService;
using Rezervise.Application.Features.Services.Commands.UpdateService;
using Rezervise.Application.Features.Services.Queries.GetServiceById;
using Rezervise.Application.Features.Services.Queries.GetServicesByBusiness;
using Rezervise.Application.Features.Services.Queries.SearchServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// İşletme hizmetleri ile ilgili işlemleri yönetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public ServiceController(IMediator mediator, ICurrentUserService currentUser)
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
        /// Yeni hizmet oluşturur.
        /// </summary>
        /// <param name="command">Hizmet bilgileri</param>
        /// <returns>Oluşturulan hizmetin ID'si</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateServiceCommand command)
        {
            // 1. OwnerId JWT'den gelir; gövde değerine güvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            // 2. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Hizmet bilgilerini günceller.
        /// </summary>
        /// <param name="command">Güncelleme bilgileri</param>
        /// <returns>Güncellenen hizmetin ID'si</returns>
        [HttpPut]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateServiceCommand command)
        {
            // 1. OwnerId JWT'den gelir; gövde değerine güvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            // 2. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Hizmeti aktif eder.
        /// </summary>
        /// <param name="id">Hizmet ID'si</param>
        /// <param name="command">Aktivasyon bilgileri</param>
        /// <returns>Aktif edilen hizmetin ID'si</returns>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Activate(Guid id, [FromBody] ActivateServiceCommand command)
        {
            // 1. ID ve sahip kimliği güvenilir kaynaklardan alınır.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.ServiceId = id;
            command.OwnerId = userId;
            command.ActivatedBy = userId.ToString();

            // 2. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Hizmeti pasif eder.
        /// </summary>
        /// <param name="id">Hizmet ID'si</param>
        /// <param name="command">Deaktivasyon bilgileri</param>
        /// <returns>Pasif edilen hizmetin ID'si</returns>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Deactivate(Guid id, [FromBody] DeactivateServiceCommand command)
        {
            // 1. ID ve sahip kimliği güvenilir kaynaklardan alınır.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.ServiceId = id;
            command.OwnerId = userId;
            command.DeactivatedBy = userId.ToString();

            // 2. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// ID'ye göre hizmet getirir.
        /// </summary>
        /// <param name="id">Hizmet ID'si</param>
        /// <returns>Hizmet detayı</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<ServiceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<ServiceDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // 1. Query'yi oluştur
            var query = new GetServiceByIdQuery { ServiceId = id };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// İşletmeye göre hizmetleri getirir.
        /// </summary>
        /// <param name="businessId">İşletme ID'si</param>
        /// <param name="isActive">Sadece aktif hizmetler mi? (opsiyonel)</param>
        /// <param name="pageNumber">Sayfa numarası (varsayılan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayılan: 10)</param>
        /// <returns>Hizmet listesi</returns>
        [HttpGet("business/{businessId}")]
        [ProducesResponseType(typeof(Result<List<ServiceDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<ServiceDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByBusiness(
            Guid businessId,
            [FromQuery] bool? isActive = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // 1. Query'yi oluştur
            var query = new GetServicesByBusinessQuery
            {
                BusinessId = businessId,
                IsActive = isActive,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
        /// <summary>
        /// Hizmet arar. İşletme, kategori veya arama terimine göre filtrelenebilir.
        /// </summary>
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<List<ServiceDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<ServiceDto>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
     [FromQuery] Guid? businessId = null,
     [FromQuery] Guid? serviceCategoryId = null,
     [FromQuery] bool? isActive = null,
     [FromQuery] string? searchTerm = null,
     [FromQuery] int pageNumber = 1,
     [FromQuery] int pageSize = 20)
        {
            var query = new SearchServicesQuery
            {
                BusinessId = businessId,
                ServiceCategoryId = serviceCategoryId,
                IsActive = isActive,
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}