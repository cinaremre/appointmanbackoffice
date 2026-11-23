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
    /// İşletme sahibinin çalışan ve izin yönetimi. Route seviyesinde
    /// BusinessOwner rolüne sınırlıdır; işletme kapsamı handler'larda
    /// doğrulanır. Onay/ret domain kurallarını mobil taraftaki mevcut
    /// handler'lardan alır.
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

        /// <summary>Sahibin çalışan listesi.</summary>
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
        /// Sahibin çalışanlarının izinleri; durum filtresiyle
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

        /// <summary>İzni onaylar; randevu çakışma kuralları domain'de yürür.</summary>
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

        /// <summary>İzni reddeder (soft delete); domain akışı tek kaynaktan yürür.</summary>
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

        /// <summary>Onay/ret istek gövdesi. Reason opsiyoneldir.</summary>
        public sealed class OwnerTimeOffDecisionRequest
        {
            /// <summary>Opsiyonel işlem gerekçesi; denetim kaydına yazılır.</summary>
            public string? Reason { get; init; }
        }
    }
}
