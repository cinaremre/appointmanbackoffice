using Rezervise.Api.Authorization;
using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.BusinessCategories.Commands.CreateCategory;
using Rezervise.Application.Features.BusinessCategories.Commands.DeleteCategory;
using Rezervise.Application.Features.BusinessCategories.Commands.UpdateCategory;
using Rezervise.Application.Features.BusinessCategories.Queries.GetAll;
using Rezervise.Application.Features.BusinessCategories.Queries.GetById;
using Rezervise.Application.Features.BusinessCategories.Queries.GetSubCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// İşletme kategorileri ile ilgili işlemleri yönetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BusinessCategoryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BusinessCategoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tüm kategorileri getirir.
        /// </summary>
        /// <param name="onlyActive">Sadece aktif kategoriler mi? (opsiyonel)</param>
        /// <returns>Kategori listesi</returns>
        [HttpGet]
        [ProducesResponseType(typeof(Result<List<BusinessCategoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<BusinessCategoryDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll([FromQuery] bool? onlyActive = null)
        {
            // 1. Query'yi oluştur
            var query = new GetAllCategoriesQuery { OnlyActive = onlyActive };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Kategori detayını getirir.
        /// </summary>
        /// <param name="id">Kategori ID'si</param>
        /// <returns>Kategori detayı</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<BusinessCategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<BusinessCategoryDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // 1. Query'yi oluştur
            var query = new GetCategoryByIdQuery { CategoryId = id };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Bir kategorinin alt kategorilerini getirir.
        /// </summary>
        /// <param name="id">Üst kategori ID'si</param>
        /// <returns>Alt kategori listesi</returns>
        [HttpGet("{id}/subcategories")]
        [ProducesResponseType(typeof(Result<List<BusinessCategoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<BusinessCategoryDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSubCategories(Guid id)
        {
            // 1. Query'yi oluştur
            var query = new GetSubCategoriesQuery { CategoryId = id };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Yeni kategori oluşturur. (Sadece Admin)
        /// </summary>
        /// <param name="command">Kategori bilgileri</param>
        /// <returns>Oluşturulan kategorinin ID'si</returns>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kategoriyi günceller. (Sadece Admin)
        /// </summary>
        /// <param name="command">Güncelleme bilgileri</param>
        /// <returns>Güncellenen kategorinin ID'si</returns>
        [HttpPut]
        [Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateCategoryCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kategoriyi siler. (Sadece Admin)
        /// </summary>
        /// <param name="id">Silinecek kategori ID'si</param>
        /// <param name="command">Silme bilgileri</param>
        /// <returns>Silinen kategorinin ID'si</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(Guid id, [FromBody] DeleteCategoryCommand command)
        {
            // 1. Command'e ID'yi ata
            command.CategoryId = id;

            // 2. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}