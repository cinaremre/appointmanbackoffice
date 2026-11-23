using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Packages.Commands.ConsumePackageSession;
using Rezervise.Application.Features.Packages.Commands.CreateServicePackage;
using Rezervise.Application.Features.Packages.Commands.PurchasePackage;
using Rezervise.Application.Features.Packages.Commands.ReleasePackageSession;
using Rezervise.Application.Features.Packages.Commands.ReservePackageSession;
using Rezervise.Application.Features.Packages.Queries.GetCustomerPackageBalance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Seans paketi işlemleri (doküman Modül 5): paket tanımı, satış,
    /// hak rezervasyonu/tüketimi/serbest bırakma ve bakiye sorgusu.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PackageController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public PackageController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Caller kimliği her zaman JWT'den çözülür; gövde değerleri yetki kaynağı değildir.
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
        /// Yeni seans paketi tanımı oluşturur. Yetki: işletme sahibi.
        /// </summary>
        /// <param name="command">Paket bilgileri ve hizmet kalemleri</param>
        /// <returns>Oluşturulan paketin ID'si</returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateServicePackageCommand command)
        {
            // Kimlik JWT'den çözülür; handler aynı kaynağı kullanır.
            if (ResolveCurrentUserId(out _) is { } unauthorized)
                return unauthorized;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Müşteriye paket satışı kaydeder. Yetki: işletme sahibi.
        /// </summary>
        /// <param name="command">Paket ve müşteri bilgileri</param>
        /// <returns>Oluşturulan müşteri paketinin ID'si</returns>
        [HttpPost("purchase")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Purchase([FromBody] PurchasePackageCommand command)
        {
            if (ResolveCurrentUserId(out _) is { } unauthorized)
                return unauthorized;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Randevu için paketten seans hakkı ayırır.
        /// </summary>
        /// <param name="command">Paket, hizmet ve randevu bilgileri</param>
        /// <returns>Oluşturulan rezervasyon hareketinin ID'si</returns>
        [HttpPost("reserve")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Reserve([FromBody] ReservePackageSessionCommand command)
        {
            if (ResolveCurrentUserId(out _) is { } unauthorized)
                return unauthorized;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Tamamlanan randevunun paket rezervasyonunu tüketime çevirir.
        /// </summary>
        /// <param name="command">Paket, hizmet ve randevu bilgileri</param>
        /// <returns>Oluşturulan tüketim hareketinin ID'si</returns>
        [HttpPost("consume")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Consume([FromBody] ConsumePackageSessionCommand command)
        {
            if (ResolveCurrentUserId(out _) is { } unauthorized)
                return unauthorized;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// İptal edilen randevunun paket rezervasyonunu serbest bırakır.
        /// </summary>
        /// <param name="command">Randevu bilgisi</param>
        /// <returns>Serbest bırakılan rezervasyon hareketinin ID'si</returns>
        [HttpPost("release")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Release([FromBody] ReleasePackageSessionCommand command)
        {
            if (ResolveCurrentUserId(out _) is { } unauthorized)
                return unauthorized;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Müşteri paketinin hak bakiyesini getirir.
        /// </summary>
        /// <param name="customerPackageId">Müşteri paketi ID'si</param>
        /// <returns>Satın alınan toplam, aktif rezervasyon, tüketilen ve kalan hak</returns>
        [HttpGet("{customerPackageId}/balance")]
        [ProducesResponseType(typeof(Result<CustomerPackageBalanceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<CustomerPackageBalanceDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBalance(Guid customerPackageId)
        {
            var query = new GetCustomerPackageBalanceQuery { CustomerPackageId = customerPackageId };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }
    }
}
