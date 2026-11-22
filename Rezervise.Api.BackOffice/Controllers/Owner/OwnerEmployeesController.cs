using Rezervise.Application.Features.Owner.Employees.Commands.ApproveMyTimeOff;
using Rezervise.Application.Features.Owner.Employees.Commands.RejectMyTimeOff;
using Rezervise.Application.Features.Owner.Employees.Queries.GetMyEmployees;
using Rezervise.Application.Features.Owner.Employees.Queries.GetMyTimeOffs;
using Rezervise.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers.Owner
{
    /// <summary>
    /// Isletme sahibinin calisan ve izin yonetimi. Route seviyesinde
    /// BusinessOwner rolune sinirlidir; isletme kapsami handler'larda
    /// dogrulanir. Onay/ret domain kurallarini mobil taraftaki mevcut
    /// handler'lardan alir.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "BusinessOwner")]
    [Route("api/v1/owner")]
    public class OwnerEmployeesController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public OwnerEmployeesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Sahibin calisan listesi.</summary>
        [HttpGet("businesses/{businessId:guid}/employees")]
        public async Task<IActionResult> GetMyEmployees(
            Guid businessId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyEmployeesQuery
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

        /// <summary>
        /// Sahibin calisanlarinin izinleri; durum filtresiyle
        /// (0=Pending, 1=Approved, 2=Rejected).
        /// </summary>
        [HttpGet("businesses/{businessId:guid}/time-offs")]
        public async Task<IActionResult> GetMyTimeOffs(
            Guid businessId,
            [FromQuery] OwnerTimeOffStatus? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyTimeOffsQuery
            {
                BusinessId = businessId,
                Status = status,
                Page = page ?? 1,
                PageSize = pageSize ?? 50
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkList(result.Value!.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>Izni onaylar; randevu cakisma kurallari domain'de yurur.</summary>
        [HttpPost("businesses/{businessId:guid}/time-offs/{timeOffId:guid}/approve")]
        public async Task<IActionResult> ApproveMyTimeOff(
            Guid businessId,
            Guid timeOffId,
            [FromBody] OwnerTimeOffDecisionRequest? body,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new ApproveMyTimeOffCommand
            {
                BusinessId = businessId,
                TimeOffId = timeOffId,
                Reason = body?.Reason
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>Izni reddeder (soft delete); domain akisi tek kaynaktan yurur.</summary>
        [HttpPost("businesses/{businessId:guid}/time-offs/{timeOffId:guid}/reject")]
        public async Task<IActionResult> RejectMyTimeOff(
            Guid businessId,
            Guid timeOffId,
            [FromBody] OwnerTimeOffDecisionRequest? body,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new RejectMyTimeOffCommand
            {
                BusinessId = businessId,
                TimeOffId = timeOffId,
                Reason = body?.Reason
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>Onay/ret istek govdesi. Reason opsiyoneldir.</summary>
        public sealed class OwnerTimeOffDecisionRequest
        {
            /// <summary>Opsiyonel islem gerekcesi; denetim kaydina yazilir.</summary>
            public string? Reason { get; init; }
        }
    }
}
