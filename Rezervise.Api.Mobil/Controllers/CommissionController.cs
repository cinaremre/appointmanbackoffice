using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Commissions.Commands.AccrueCommission;
using Rezervise.Application.Features.Commissions.Commands.CreateCommissionRule;
using Rezervise.Application.Features.Commissions.Commands.ReverseCommissionAccrual;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Personel prim (komisyon) yönetimi: kural tanımı, tahakkuk ve ters çevirme.
    /// Ödeme dönemi (payout) işlemleri ilk sürün kapsamında değildir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CommissionController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public CommissionController(IMediator mediator, ICurrentUserService currentUser)
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
        /// Yeni prim kuralı tanımlar. Yüzde tiplerinde Rate 0-1 arası verilir.
        /// </summary>
        /// <param name="command">Kural bilgileri</param>
        /// <returns>Oluşturulan kuralın ID'si</returns>
        [HttpPost("rule")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRule([FromBody] CreateCommissionRuleCommand command)
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
        /// Çalışana prim tahakkuk eder. Tahakkuk tahsilat tabanlıdır: BaseAmount
        /// alani tahsil edilen tutar olarak verilmelidir.
        /// </summary>
        /// <param name="command">Tahakkuk bilgileri</param>
        /// <returns>Oluşturulan tahakkukun ID'si</returns>
        [HttpPost("accrue")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Accrue([FromBody] AccrueCommissionCommand command)
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
        /// Prim tahakkukunu ters çevirir. Ödenmiş (Paid) tahakkuklar reddedilir.
        /// </summary>
        /// <param name="command">Ters çevirme bilgileri</param>
        /// <returns>Ters çevrilen tahakkukun ID'si</returns>
        [HttpPost("reverse")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Reverse([FromBody] ReverseCommissionAccrualCommand command)
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
    }
}
