using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Blacklists.Commands.AddToBlacklist;
using AppointMan.Application.Features.Blacklists.Commands.RemoveFromBlacklist;
using AppointMan.Application.Features.Blacklists.Queries.GetBlacklistedUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppointMan.API.Controllers
{
    /// <summary>
    /// Kara liste islemlerini yonetir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BlacklistController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BlacklistController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Manuel olarak kara listeye ekler.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Add([FromBody] AddToBlacklistCommand command)
        {
            var requesterId = GetRequesterId();
            if (requesterId == Guid.Empty)
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Gecersiz kullanici.")));

            command.RequesterId = requesterId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Isletmenin kara listesini getirir.
        /// </summary>
        [HttpGet("business/{businessId}")]
        [ProducesResponseType(typeof(Result<List<BlacklistEntryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<List<BlacklistEntryDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByBusiness(
            Guid businessId,
            [FromQuery] bool includeRemoved = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var requesterId = GetRequesterId();
            if (requesterId == Guid.Empty)
                return Unauthorized(Result<List<BlacklistEntryDto>>.Failure(
                    Error.Unauthorized("Gecersiz kullanici.")));

            var query = new GetBlacklistedUsersQuery
            {
                BusinessId = businessId,
                RequesterId = requesterId,
                IncludeRemoved = includeRemoved,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Kullaniciyi kara listeden cikarir (soft delete).
        /// </summary>
        [HttpPost("{id}/remove")]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Remove(Guid id, [FromBody] RemoveFromBlacklistCommand command)
        {
            var requesterId = GetRequesterId();
            if (requesterId == Guid.Empty)
                return Unauthorized(Result<Guid>.Failure(Error.Unauthorized("Gecersiz kullanici.")));

            command.BlacklistEntryId = id;
            command.RequesterId = requesterId;

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        private Guid GetRequesterId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Guid.Empty;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}