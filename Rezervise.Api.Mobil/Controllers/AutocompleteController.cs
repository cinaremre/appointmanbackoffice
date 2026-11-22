using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Autocomplete.Queries.Search;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Tek bir uctan adres hiyerarsisi ve kategori agaclari icin otomatik
    /// tamamlama saglar. Mobil istemci sonucu ID ile birlikte alir ve bu ID'yi
    /// ilgili endpoint'e aynen geri gonderir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AutocompleteController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AutocompleteController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Verilen tip icinde en az 3 karakterlik arama yapar ve ID'li sonuc listesi doner.
        /// </summary>
        /// <param name="type">
        /// Kaynak tipi: city, district, locality, neighborhood, businessCategory, serviceCategory.
        /// </param>
        /// <param name="q">Arama terimi (en az 3 karakter, buyuk/kucuk harf duyarsiz).</param>
        /// <param name="parentId">
        /// Ust birim ID'si (opsiyonel). Verilirse sonuclar o ust birime daraltilir;
        /// bos birakilirsa arama tum Turkiye genelinde yapilir. Adres tipleri icin
        /// sayisal, kategori tipleri icin Guid biciminde olmalidir.
        /// </param>
        /// <param name="limit">Donulecek en fazla kayit sayisi (1-25, varsayilan: 10).</param>
        /// <returns>ID ve gorunen ad iceren otomatik tamamlama listesi.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(Result<List<AutocompleteItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<AutocompleteItemDto>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
            [FromQuery] string type,
            [FromQuery] string q,
            [FromQuery] string? parentId = null,
            [FromQuery] int limit = 10)
        {
            // 1. Query'yi olustur
            var query = new AutocompleteQuery
            {
                Type = type,
                Query = q,
                ParentId = parentId,
                Limit = limit
            };

            // 2. Mediator ile gonder (dogrulama pipeline'da calisir)
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
