using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Reviews.Commands.AddReview;
using AppointMan.Application.Features.Reviews.Commands.DeleteReview;
using AppointMan.Application.Features.Reviews.Commands.HideReview;
using AppointMan.Application.Features.Reviews.Commands.ShowReview;
using AppointMan.Application.Features.Reviews.Commands.UpdateReview;
using AppointMan.Application.Features.Reviews.Queries.GetByBusiness;
using AppointMan.Application.Features.Reviews.Queries.GetById;
using AppointMan.Application.Features.Reviews.Queries.GetByUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.Api.Controllers
{
    /// <summary>
    /// Isletme yorumlari ile ilgili islemleri yonetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReviewController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Isletmenin tum yorumlarini getirir.
        /// </summary>
        /// <param name="businessId">Isletme ID'si</param>
        /// <param name="pageNumber">Sayfa numarasi (varsayilan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayilan: 10)</param>
        /// <returns>Yorum listesi</returns>
        [HttpGet("business/{businessId}")]
        [ProducesResponseType(typeof(Result<List<ReviewDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<ReviewDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByBusiness(
            Guid businessId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // 1. Query'yi olustur
            var query = new GetReviewsByBusinessQuery
            {
                BusinessId = businessId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Kullanicinin tum yorumlarini getirir.
        /// </summary>
        /// <param name="userId">Kullanici ID'si</param>
        /// <param name="pageNumber">Sayfa numarasi (varsayilan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayilan: 10)</param>
        /// <returns>Yorum listesi</returns>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(Result<List<ReviewDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<ReviewDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByUser(
            Guid userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // 1. Query'yi olustur
            var query = new GetReviewsByUserQuery
            {
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Yorum detayini getirir.
        /// </summary>
        /// <param name="id">Yorum ID'si</param>
        /// <returns>Yorum detayi</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<ReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<ReviewDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // 1. Query'yi olustur
            var query = new GetReviewByIdQuery { ReviewId = id };

            // 2. Mediator ile gonder
            var result = await _mediator.Send(query);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Yeni yorum ekler.
        /// </summary>
        /// <param name="command">Yorum bilgileri</param>
        /// <returns>Olusturulan yorumun ID'si</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Add([FromBody] AddReviewCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Yorumu gunceller.
        /// </summary>
        /// <param name="command">Guncelleme bilgileri</param>
        /// <returns>Guncellenen yorumun ID'si</returns>
        [HttpPut]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateReviewCommand command)
        {
            // 1. Command'i mediator ile gonder
            var result = await _mediator.Send(command);

            // 2. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Yorumu siler.
        /// </summary>
        /// <param name="id">Yorum ID'si</param>
        /// <param name="command">Silme bilgileri</param>
        /// <returns>Silinen yorumun ID'si</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(Guid id, [FromBody] DeleteReviewCommand command)
        {
            // 1. ID'yi command'e ata
            command.ReviewId = id;

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Yorumu gizler. (Sadece Admin)
        /// </summary>
        /// <param name="id">Yorum ID'si</param>
        /// <param name="command">Gizleme bilgileri</param>
        /// <returns>Gizlenen yorumun ID'si</returns>
        [HttpPost("{id}/hide")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Hide(Guid id, [FromBody] HideReviewCommand command)
        {
            // 1. ID'yi command'e ata
            command.ReviewId = id;

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Gizlenen yorumu tekrar gosterir. (Sadece Admin)
        /// </summary>
        /// <param name="id">Yorum ID'si</param>
        /// <param name="command">Gosterme bilgileri</param>
        /// <returns>Gosterilen yorumun ID'si</returns>
        [HttpPost("{id}/show")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Show(Guid id, [FromBody] ShowReviewCommand command)
        {
            // 1. ID'yi command'e ata
            command.ReviewId = id;

            // 2. Mediator ile gonder
            var result = await _mediator.Send(command);

            // 3. Sonuca gore cevap dondur
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}