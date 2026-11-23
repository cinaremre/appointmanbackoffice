using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Autocomplete.Queries.Search;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Tek bir uçtan adres hiyerarşisi ve kategori ağaçları için otomatik
    /// tamamlama sağlar. Mobil istemci sonucu ID ile birlikte alır ve bu ID'yi
    /// ilgili endpoint'e aynen geri gönderir.
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
        /// Verilen tip içinde en az 3 karakterlik arama yapar ve ID'li sonuç listesi döner.
        /// </summary>
        /// <param name="type">
        /// Kaynak tipi: city, district, locality, neighborhood, businessCategory, serviceCategory.
        /// </param>
        /// <param name="q">Arama terimi (en az 3 karakter, büyük/küçük harf duyarsız).</param>
        /// <param name="parentId">
        /// Üst birim ID'si (opsiyonel). Verilirse sonuçlar o üst birime daraltılır;
        /// boş bırakılırsa arama tüm Türkiye genelinde yapılır. Adres tipleri için
        /// sayısal, kategori tipleri için Guid biçiminde olmalıdır.
        /// </param>
        /// <param name="limit">Dönülecek en fazla kayıt sayısı (1-25, varsayılan: 10).</param>
        /// <returns>ID ve görünen ad içeren otomatik tamamlama listesi.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(Result<List<AutocompleteItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<AutocompleteItemDto>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
            [FromQuery] string type,
            [FromQuery] string q,
            [FromQuery] string? parentId = null,
            [FromQuery] int limit = 10)
        {
            // 1. Query'yi oluştur
            var query = new AutocompleteQuery
            {
                Type = type,
                Query = q,
                ParentId = parentId,
                Limit = limit
            };

            // 2. Mediator ile gönder (doğrulama pipeline'da çalışır)
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
