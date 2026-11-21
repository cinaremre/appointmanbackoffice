using AppointMan.Application.Features.Owner.Reviews.Queries.GetMyReviews;
using AppointMan.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.BackOffice.Api.Controllers.Owner
{
    /// <summary>
    /// Isletme sahibinin yorum listesi. Route seviyesinde BusinessOwner
    /// rolune sinirlidir; isletme kapsami handler'da dogrulanir. Uc salt
    /// okunurdur; gizleme domain komutlariyla yapilir.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "BusinessOwner")]
    [Route("api/v1/owner")]
    public class OwnerReviewsController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public OwnerReviewsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Sahibin isletmesine yapilan yorumlar; en yeni once, gizli
        /// yorumlar dahil listelenir.
        /// </summary>
        [HttpGet("businesses/{businessId:guid}/reviews")]
        public async Task<IActionResult> GetMyReviews(
            Guid businessId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyReviewsQuery
            {
                BusinessId = businessId,
                Page = page ?? 1,
                PageSize = pageSize ?? 50
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkList(result.Value!.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }
    }
}
