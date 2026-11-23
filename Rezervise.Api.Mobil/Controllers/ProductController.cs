using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Inventory.Commands.ConsumeServiceMaterials;
using Rezervise.Application.Features.Inventory.Commands.CreateProduct;
using Rezervise.Application.Features.Inventory.Commands.DefineServiceMaterial;
using Rezervise.Application.Features.Inventory.Commands.RecordStockMovement;
using Rezervise.Application.Features.Inventory.Queries.GetLowStockProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Stok yönetimi: ürün tanımı, stok hareketleri, hizmet reçeteleri,
    /// randevu malzeme tüketimi ve kritik stok raporu.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public ProductController(IMediator mediator, ICurrentUserService currentUser)
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
        /// İşletmeye yeni stok ürünü tanımlar. SKU işletme bazında benzersizdir.
        /// </summary>
        /// <param name="command">Ürün bilgileri</param>
        /// <returns>Oluşturulan ürünün ID'si</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
        {
            // 1. OwnerId JWT'den gelir; gövde değerine güvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            // 2. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Stok hareketi kaydeder (satın alma, satış, zayı, sayım, ...).
        /// Miktar pozitif verilir; çıkış hareketleri bakiyeyi eksiye düşürüyorsa
        /// istek reddedilir.
        /// </summary>
        /// <param name="command">Hareket bilgileri</param>
        /// <returns>Oluşturulan hareketin ID'si</returns>
        [HttpPost("stock-movement")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RecordStockMovement([FromBody] RecordStockMovementCommand command)
        {
            // 1. OwnerId JWT'den gelir; gövde değerine güvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            // 2. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Hizmet reçetesi tanımlar: hizmet gerçeleştiğinde tüketilecek malzeme.
        /// </summary>
        /// <param name="command">Reçete bilgileri</param>
        /// <returns>Oluşturulan reçete kaydının ID'si</returns>
        [HttpPost("service-material")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DefineServiceMaterial([FromBody] DefineServiceMaterialCommand command)
        {
            // 1. OwnerId JWT'den gelir; gövde değerine güvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            // 2. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Tamamlanmış randevunun gerçek malzeme tüketimini stok hareketlerine işler.
        /// Aynı randevu için tüketim ikinci kez işlenemez.
        /// </summary>
        /// <param name="command">Tüketim listesi</param>
        /// <returns>Oluşturulan hareket sayısı</returns>
        [HttpPost("consume")]
        [ProducesResponseType(typeof(Result<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConsumeServiceMaterials([FromBody] ConsumeServiceMaterialsCommand command)
        {
            // 1. OwnerId JWT'den gelir; gövde değerine güvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            // 2. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Bakiyesi kritik stok seviyesinin altına düşmüş aktif ürünleri listeler.
        /// </summary>
        /// <param name="businessId">İşletme ID'si</param>
        /// <returns>Kritik seviyedeki ürün listesi</returns>
        [HttpGet("low-stock/{businessId}")]
        [ProducesResponseType(typeof(Result<List<LowStockProductDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<LowStockProductDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLowStock(Guid businessId)
        {
            // 1. Query'yi oluştur
            var query = new GetLowStockProductsQuery { BusinessId = businessId };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}
