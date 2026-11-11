using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.ServiceCategories.Queries.GetById;
using AppointMan.Application.Features.ServiceCategories.Queries.GetFlat;
using AppointMan.Application.Features.ServiceCategories.Queries.GetSubCategories;
using AppointMan.Application.Features.ServiceCategories.Queries.GetTree;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Hizmet kategorileri ile ilgili islemleri yonetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class ServiceCategoryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ServiceCategoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tum kategorileri agac yapisinda getirir.
        /// </summary>
        /// <param name="lang">Dil kodu (tr, en, de...)</param>
        /// <param name="onlyActive">Sadece aktif kategoriler mi?</param>
        [HttpGet("tree")]
        [ProducesResponseType(typeof(Result<List<ServiceCategoryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTree(
            [FromQuery] string lang = "tr",
            [FromQuery] bool onlyActive = true)
        {
            var query = new GetServiceCategoryTreeQuery
            {
                LanguageCode = lang,
                OnlyActive = onlyActive
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Tum kategorileri duz liste olarak getirir.
        /// </summary>
        /// <param name="lang">Dil kodu</param>
        /// <param name="onlyActive">Sadece aktif kategoriler mi?</param>
        /// <param name="onlyLeaves">Sadece leaf kategoriler mi?</param>
        /// <param name="parentCategoryId">Belirli bir ust kategorinin alt kategorileri</param>
        [HttpGet("flat")]
        [ProducesResponseType(typeof(Result<List<ServiceCategoryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFlat(
            [FromQuery] string lang = "tr",
            [FromQuery] bool onlyActive = true,
            [FromQuery] bool onlyLeaves = false,
            [FromQuery] Guid? parentCategoryId = null)
        {
            var query = new GetServiceCategoriesFlatQuery
            {
                LanguageCode = lang,
                OnlyActive = onlyActive,
                OnlyLeaves = onlyLeaves,
                ParentCategoryId = parentCategoryId
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// ID'ye gore kategori detayi getirir.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<ServiceCategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<ServiceCategoryDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] string lang = "tr")
        {
            var query = new GetServiceCategoryByIdQuery
            {
                CategoryId = id,
                LanguageCode = lang
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Bir kategorinin alt kategorilerini getirir.
        /// </summary>
        [HttpGet("{id}/subcategories")]
        [ProducesResponseType(typeof(Result<List<ServiceCategoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<ServiceCategoryDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSubCategories(
            Guid id,
            [FromQuery] string lang = "tr",
            [FromQuery] bool onlyActive = true)
        {
            var query = new GetServiceCategorySubCategoriesQuery
            {
                CategoryId = id,
                LanguageCode = lang,
                OnlyActive = onlyActive
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}