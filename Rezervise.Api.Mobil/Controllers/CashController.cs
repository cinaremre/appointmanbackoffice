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
    /// Kasa tanımı ve oturum işlemlerini yönetir. Yetki kontrolleri handler
    /// katmanında JWT kimliği ve işletme sahibi üzerinden yapılır.
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
        /// İşletmeye yeni kasa tanımlar.
        /// </summary>
        /// <param name="command">Kasa bilgileri</param>
        /// <returns>Oluşturulan kasanın ID'si</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRegister([FromBody] CreateCashRegisterCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kasada yeni oturum açar.
        /// </summary>
        /// <param name="command">Açılış bilgileri</param>
        /// <returns>Açılan oturumun ID'si</returns>
        [HttpPost("open")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> OpenSession([FromBody] OpenCashSessionCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Açık oturumu kapatır; beklenen/sayılan nakit farkını rapolar.
        /// </summary>
        /// <param name="command">Kapanış bilgileri</param>
        /// <returns>Kapanış özeti</returns>
        [HttpPost("close")]
        [ProducesResponseType(typeof(Result<CashSessionCloseResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<CashSessionCloseResult>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CloseSession([FromBody] CloseCashSessionCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Açık oturuma manuel nakit girişi/çıkışı ekler.
        /// </summary>
        /// <param name="command">Hareket bilgileri</param>
        /// <returns>Oluşturulan hareketin ID'si</returns>
        [HttpPost("movement")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddMovement([FromBody] AddCashMovementCommand command)
        {
            // 1. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 2. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kasayı devre dışı bırakır.
        /// </summary>
        /// <param name="id">Kasa ID'si</param>
        /// <returns>Güncellenen kasanın ID'si</returns>
        [HttpPost("register/{id}/deactivate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeactivateRegister(Guid id)
        {
            // 1. ID güvenilir kaynaktan (route) alınır
            var command = new SetCashRegisterActiveCommand { CashRegisterId = id, IsActive = false };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Deaktif kasayı tekrar aktif eder.
        /// </summary>
        /// <param name="id">Kasa ID'si</param>
        /// <returns>Güncellenen kasanın ID'si</returns>
        [HttpPost("register/{id}/activate")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateRegister(Guid id)
        {
            // 1. ID güvenilir kaynaktan (route) alınır
            var command = new SetCashRegisterActiveCommand { CashRegisterId = id, IsActive = true };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
