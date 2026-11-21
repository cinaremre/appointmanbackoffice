using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Packages.Commands.ConsumePackageSession;
using AppointMan.Application.Features.Packages.Commands.CreateServicePackage;
using AppointMan.Application.Features.Packages.Commands.PurchasePackage;
using AppointMan.Application.Features.Packages.Commands.ReleasePackageSession;
using AppointMan.Application.Features.Packages.Commands.ReservePackageSession;
using AppointMan.Application.Features.Packages.Queries.GetCustomerPackageBalance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Seans paketi islemleri (dokuman Modul 5): paket tanimi, satis,
    /// hak rezervasyonu/tuketimi/serbest birakma ve bakiye sorgusu.
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
        /// Caller kimligi her zaman JWT'den cozulur; govde degerleri yetki kaynagi degildir.
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
        /// Yeni seans paketi tanimi olusturur. Yetki: isletme sahibi.
        /// </summary>
        /// <param name="command">Paket bilgileri ve hizmet kalemleri</param>
        /// <returns>Olusturulan paketin ID'si</returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateServicePackageCommand command)
        {
            // Kimlik JWT'den cozulur; handler ayni kaynagi kullanir.
            if (ResolveCurrentUserId(out _) is { } unauthorized)
                return unauthorized;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Musteriye paket satisi kaydeder. Yetki: isletme sahibi.
        /// </summary>
        /// <param name="command">Paket ve musteri bilgileri</param>
        /// <returns>Olusturulan musteri paketinin ID'si</returns>
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
        /// Randevu icin paketten seans hakki ayirir.
        /// </summary>
        /// <param name="command">Paket, hizmet ve randevu bilgileri</param>
        /// <returns>Olusturulan rezervasyon hareketinin ID'si</returns>
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
        /// Tamamlanan randevunun paket rezervasyonunu tuketime cevirir.
        /// </summary>
        /// <param name="command">Paket, hizmet ve randevu bilgileri</param>
        /// <returns>Olusturulan tuketim hareketinin ID'si</returns>
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
        /// Iptal edilen randevunun paket rezervasyonunu serbest birakir.
        /// </summary>
        /// <param name="command">Randevu bilgisi</param>
        /// <returns>Serbest birakilan rezervasyon hareketinin ID'si</returns>
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
        /// Musteri paketinin hak bakiyesini getirir.
        /// </summary>
        /// <param name="customerPackageId">Musteri paketi ID'si</param>
        /// <returns>Satin alinan toplam, aktif rezervasyon, tuketilen ve kalan hak</returns>
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
