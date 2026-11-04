using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.BusinessCategories.Commands.CreateCategory;
using AppointMan.Application.Features.BusinessCategories.Commands.DeleteCategory;
using AppointMan.Application.Features.BusinessCategories.Commands.UpdateCategory;
using AppointMan.Application.Features.BusinessCategories.Queries.GetAll;
using AppointMan.Application.Features.BusinessCategories.Queries.GetById;
using AppointMan.Application.Features.BusinessCategories.Queries.GetSubCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Isletme kategorileri ile ilgili islemleri yonetir.
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
        /// Tum kategorileri getirir.
        /// </summary>
        /// <param name="onlyActive">Sadece aktif kategoriler mi? (opsiyonel)</param>
        /// <returns>Kategori listesi</returns>
        [HttpGet]
        [ProducesResponseType(typeof(Result<List<BusinessCategoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<BusinessCategoryDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll([FromQuery] bool? onlyActive = null)
        {
            // 1. Query'yi olustur
            var query = new GetAllCategoriesQuery { OnlyActive = onlyActive };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Kategori detayini getirir.
        /// </summary>
        /// <param name="id">Kategori ID'si</param>
        /// <returns>Kategori detayi</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<BusinessCategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<BusinessCategoryDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // 1. Query'yi olustur
            var query = new GetCategoryByIdQuery { CategoryId = id };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Bir kategorinin alt kategorilerini getirir.
        /// </summary>
        /// <param name="id">Ust kategori ID'si</param>
        /// <returns>Alt kategori listesi</returns>
        [HttpGet("{id}/subcategories")]
        [ProducesResponseType(typeof(Result<List<BusinessCategoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<BusinessCategoryDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSubCategories(Guid id)
        {
            // 1. Query'yi olustur
            var query = new GetSubCategoriesQuery { CategoryId = id };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Yeni kategori olusturur. (Sadece Admin)
        /// </summary>
        /// <param name="command">Kategori bilgileri</param>
        /// <returns>Olusturulan kategorinin ID'si</returns>
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kategoriyi gunceller. (Sadece Admin)
        /// </summary>
        /// <param name="command">Guncelleme bilgileri</param>
        /// <returns>Guncellenen kategorinin ID'si</returns>
        [HttpPut]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateCategoryCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
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
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(Guid id, [FromBody] DeleteCategoryCommand command)
        {
            // 1. Command'e ID'yi ata
            command.CategoryId = id;

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}