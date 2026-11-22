using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Cash.Commands.AddCashMovement;
using Rezervise.Application.Features.Cash.Commands.CloseCashSession;
using Rezervise.Application.Features.Cash.Commands.CreateCashRegister;
using Rezervise.Application.Features.Cash.Commands.OpenCashSession;
using Rezervise.Application.Features.Cash.Commands.SetCashRegisterActive;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// Kasa tanimi ve oturum islemlerini yonetir. Yetki kontrolleri handler
    /// katmaninda JWT kimligi ve isletme sahibi uzerinden yapilir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CashController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CashController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Isletmeye yeni kasa tanimlar.
        /// </summary>
        /// <param name="command">Kasa bilgileri</param>
        /// <returns>Olusturulan kasanin ID'si</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRegister([FromBody] CreateCashRegisterCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kasada yeni oturum acar.
        /// </summary>
        /// <param name="command">Acilis bilgileri</param>
        /// <returns>Acilan oturumun ID'si</returns>
        [HttpPost("open")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> OpenSession([FromBody] OpenCashSessionCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Acik oturumu kapatir; beklenen/sayilan nakit farkini rapolar.
        /// </summary>
        /// <param name="command">Kapanis bilgileri</param>
        /// <returns>Kapanis ozeti</returns>
        [HttpPost("close")]
        [ProducesResponseType(typeof(Result<CashSessionCloseResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<CashSessionCloseResult>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CloseSession([FromBody] CloseCashSessionCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Acik oturuma manuel nakit girisi/cikisi ekler.
        /// </summary>
        /// <param name="command">Hareket bilgileri</param>
        /// <returns>Olusturulan hareketin ID'si</returns>
        [HttpPost("movement")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddMovement([FromBody] AddCashMovementCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kasayi devre disi birakir.
        /// </summary>
        /// <param name="id">Kasa ID'si</param>
        /// <returns>Guncellenen kasanin ID'si</returns>
        [HttpPost("register/{id}/deactivate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateRegister(Guid id)
        {
            // 1. ID guvenilir kaynaktan (route) alinir
            var command = new SetCashRegisterActiveCommand { CashRegisterId = id, IsActive = false };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Deaktif kasayi tekrar aktif eder.
        /// </summary>
        /// <param name="id">Kasa ID'si</param>
        /// <returns>Guncellenen kasanin ID'si</returns>
        [HttpPost("register/{id}/activate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateRegister(Guid id)
        {
            // 1. ID guvenilir kaynaktan (route) alinir
            var command = new SetCashRegisterActiveCommand { CashRegisterId = id, IsActive = true };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
