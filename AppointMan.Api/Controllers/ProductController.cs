using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Inventory.Commands.ConsumeServiceMaterials;
using AppointMan.Application.Features.Inventory.Commands.CreateProduct;
using AppointMan.Application.Features.Inventory.Commands.DefineServiceMaterial;
using AppointMan.Application.Features.Inventory.Commands.RecordStockMovement;
using AppointMan.Application.Features.Inventory.Queries.GetLowStockProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Stok yonetimi: urun tanimi, stok hareketleri, hizmet receteleri,
    /// randevu malzeme tuketimi ve kritik stok raporu.
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
        /// Isletmeye yeni stok urunu tanimlar. SKU isletme bazinda benzersizdir.
        /// </summary>
        /// <param name="command">Urun bilgileri</param>
        /// <returns>Olusturulan urunun ID'si</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
        {
            // 1. OwnerId JWT'den gelir; govde degerine guvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            // 2. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Stok hareketi kaydeder (satin alma, satis, zayi, sayim, ...).
        /// Miktar pozitif verilir; cikis hareketleri bakiyeyi eksiye dusuruyorsa
        /// istek reddedilir.
        /// </summary>
        /// <param name="command">Hareket bilgileri</param>
        /// <returns>Olusturulan hareketin ID'si</returns>
        [HttpPost("stock-movement")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RecordStockMovement([FromBody] RecordStockMovementCommand command)
        {
            // 1. OwnerId JWT'den gelir; govde degerine guvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            // 2. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Hizmet recetesi tanimlar: hizmet gercelestiginde tuketilecek malzeme.
        /// </summary>
        /// <param name="command">Recete bilgileri</param>
        /// <returns>Olusturulan recete kaydinin ID'si</returns>
        [HttpPost("service-material")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DefineServiceMaterial([FromBody] DefineServiceMaterialCommand command)
        {
            // 1. OwnerId JWT'den gelir; govde degerine guvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            // 2. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Tamamlanmis randevunun gercek malzeme tuketimini stok hareketlerine isler.
        /// Ayni randevu icin tuketim ikinci kez islenemez.
        /// </summary>
        /// <param name="command">Tuketim listesi</param>
        /// <returns>Olusturulan hareket sayisi</returns>
        [HttpPost("consume")]
        [ProducesResponseType(typeof(Result<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<int>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConsumeServiceMaterials([FromBody] ConsumeServiceMaterialsCommand command)
        {
            // 1. OwnerId JWT'den gelir; govde degerine guvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.OwnerId = userId;

            // 2. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Bakiyesi kritik stok seviyesinin altina dusmus aktif urunleri listeler.
        /// </summary>
        /// <param name="businessId">Isletme ID'si</param>
        /// <returns>Kritik seviyedeki urun listesi</returns>
        [HttpGet("low-stock/{businessId}")]
        [ProducesResponseType(typeof(Result<List<LowStockProductDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<LowStockProductDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLowStock(Guid businessId)
        {
            // 1. Query'yi olustur
            var query = new GetLowStockProductsQuery { BusinessId = businessId };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}
