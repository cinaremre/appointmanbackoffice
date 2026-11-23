using Rezervise.Api.Authorization;
using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Reviews.Commands.AddReview;
using Rezervise.Application.Features.Reviews.Commands.DeleteReview;
using Rezervise.Application.Features.Reviews.Commands.HideReview;
using Rezervise.Application.Features.Reviews.Commands.ShowReview;
using Rezervise.Application.Features.Reviews.Commands.UpdateReview;
using Rezervise.Application.Features.Reviews.Queries.GetByBusiness;
using Rezervise.Application.Features.Reviews.Queries.GetById;
using Rezervise.Application.Features.Reviews.Queries.GetByUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.Api.Controllers
{
    /// <summary>
    /// İşletme yorumları ile ilgili işlemleri yönetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public ReviewController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Token'dan çözülen kullanıcı ID'sını döndürür; eksikse Unauthorized üretir.
        /// İstek gövdesindeki UserId değerleri yetki için kullanılmaz.
        /// </summary>
        /// <param name="userId">Doğrulanmış kullanıcı ID'si.</param>
        /// <returns>Kimlik eksikse dönülecek sonuç; aksi halde null.</returns>
        private IActionResult? ResolveCurrentUserId(out Guid userId)
        {
            var currentUserId = _currentUser.UserId;
            if (!_currentUser.IsAuthenticated || currentUserId is null)
            {
                userId = Guid.Empty;
                return Unauthorized();
            }

            userId = currentUserId.Value;
            return null;
        }

        /// <summary>
        /// İşletmenin tüm yorumlarını getirir.
        /// </summary>
        /// <param name="businessId">İşletme ID'si</param>
        /// <param name="pageNumber">Sayfa numarası (varsayılan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayılan: 10)</param>
        /// <returns>Yorum listesi</returns>
        [HttpGet("business/{businessId}")]
        [ProducesResponseType(typeof(Result<List<ReviewDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<ReviewDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByBusiness(
            Guid businessId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // 1. Query'yi oluştur
            var query = new GetReviewsByBusinessQuery
            {
                BusinessId = businessId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Kullanıcının tüm yorumlarını getirir.
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <param name="pageNumber">Sayfa numarası (varsayılan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayılan: 10)</param>
        /// <returns>Yorum listesi</returns>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(Result<List<ReviewDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<ReviewDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByUser(
            Guid userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            // 1. Query'yi oluştur
            var query = new GetReviewsByUserQuery
            {
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Yorum detayını getirir.
        /// </summary>
        /// <param name="id">Yorum ID'si</param>
        /// <returns>Yorum detayı</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<ReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<ReviewDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // 1. Query'yi oluştur
            var query = new GetReviewByIdQuery { ReviewId = id };

            // 2. Mediator ile gönder
            var result = await _mediator.Send(query);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        /// <summary>
        /// Yeni yorum ekler.
        /// </summary>
        /// <param name="command">Yorum bilgileri</param>
        /// <returns>Oluşturulan yorumun ID'si</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Add([FromBody] AddReviewCommand command)
        {
            // 1. UserId her zaman JWT'den alınır; gövde değerine güvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.UserId = userId;

            // 2. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Yorumu günceller.
        /// </summary>
        /// <param name="command">Güncelleme bilgileri</param>
        /// <returns>Güncellenen yorumun ID'si</returns>
        [HttpPut]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromBody] UpdateReviewCommand command)
        {
            // 1. UserId her zaman JWT'den alınır; gövde değerine güvenilmez.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.UserId = userId;

            // 2. Command'i mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
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
            // 1. ID ve aktör kimliği güvenilir kaynaklardan alınır.
            if (ResolveCurrentUserId(out var userId) is { } unauthorized)
                return unauthorized;

            command.ReviewId = id;
            command.UserId = userId;

            // 2. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
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
        [Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Hide(Guid id, [FromBody] HideReviewCommand command)
        {
            // 1. ID'yi command'e ata
            command.ReviewId = id;

            // 2. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Gizlenen yorumu tekrar gösterir. (Sadece Admin)
        /// </summary>
        /// <param name="id">Yorum ID'si</param>
        /// <param name="command">Gösterme bilgileri</param>
        /// <returns>Gösterilen yorumun ID'si</returns>
        [HttpPost("{id}/show")]
        [Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Show(Guid id, [FromBody] ShowReviewCommand command)
        {
            // 1. ID'yi command'e ata
            command.ReviewId = id;

            // 2. Mediator ile gönder
            var result = await _mediator.Send(command);

            // 3. Sonuça göre cevap döndür
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}