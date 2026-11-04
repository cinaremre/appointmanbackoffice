using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Businesses.Commands.ActivateBusiness;
using AppointMan.Application.Features.Businesses.Commands.CreateBusiness;
using AppointMan.Application.Features.Businesses.Commands.DeactivateBusiness;
using AppointMan.Application.Features.Businesses.Commands.UpdateBusiness;
using AppointMan.Application.Features.Businesses.Queries.GetBusinessById;
using AppointMan.Application.Features.Businesses.Queries.GetBusinessesByOwner;
using AppointMan.Application.Features.Businesses.Queries.SearchBusinesses;
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

        public BusinessController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Yeni isletme olusturur.
        /// </summary>
        /// <param name="command">Isletme bilgileri</param>
        /// <returns>Olusturulan isletmenin ID'si</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateBusinessCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Isletme bilgilerini gunceller.
        /// </summary>
        /// <param name="command">Guncelleme bilgileri</param>
        /// <returns>Guncellenen isletmenin ID'si</returns>
        [HttpPut]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateBusinessCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Isletmeyi aktif eder.
        /// </summary>
        /// <param name="id">Isletme ID'si</param>
        /// <param name="command">Aktivasyon bilgileri</param>
        /// <returns>Aktif edilen isletmenin ID'si</returns>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Activate(Guid id, [FromBody] ActivateBusinessCommand command)
        {
            // 1. ID'yi command'e ata
            command.BusinessId = id;

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Isletmeyi pasif eder.
        /// </summary>
        /// <param name="id">Isletme ID'si</param>
        /// <param name="command">Deaktivasyon bilgileri</param>
        /// <returns>Pasif edilen isletmenin ID'si</returns>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Deactivate(Guid id, [FromBody] DeactivateBusinessCommand command)
        {
            // 1. ID'yi command'e ata
            command.BusinessId = id;

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// ID'ye gore isletme getirir.
        /// </summary>
        /// <param name="id">Isletme ID'si</param>
        /// <returns>Isletme detaylari</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<BusinessDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<BusinessDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // 1. Query'yi olustur
            var query = new GetBusinessByIdQuery { BusinessId = id };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Sahibine gore isletmeleri getirir.
        /// </summary>
        /// <param name="ownerId">Isletme sahibinin ID'si</param>
        /// <param name="pageNumber">Sayfa numarasi (varsayilan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayilan: 10)</param>
        /// <returns>Isletme listesi</returns>
        [HttpGet("owner/{ownerId}")]
        [ProducesResponseType(typeof(Result<List<BusinessDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<BusinessDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByOwner(
            Guid ownerId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // 1. Query'yi olustur
            var query = new GetBusinessesByOwnerQuery
            {
                OwnerId = ownerId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Kategori, sehir ve ilceye gore isletme arar.
        /// </summary>
        /// <param name="categoryId">Kategori ID'si (opsiyonel)</param>
        /// <param name="city">Sehir (zorunlu)</param>
        /// <param name="district">Ilce (opsiyonel)</param>
        /// <param name="pageNumber">Sayfa numarasi (varsayilan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayilan: 20)</param>
        /// <returns>Isletme listesi</returns>
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<List<BusinessDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<BusinessDto>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
            [FromQuery] Guid? categoryId,
            [FromQuery] string city,
            [FromQuery] string? district,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = new SearchBusinessesQuery
            {
                CategoryId = categoryId,
                City = city,
                District = district,
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