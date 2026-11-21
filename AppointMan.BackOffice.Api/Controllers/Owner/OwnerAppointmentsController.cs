using AppointMan.Application.Features.Owner.Appointments.Commands.CancelMyAppointment;
using AppointMan.Application.Features.Owner.Appointments.Queries.GetMyAppointments;
using AppointMan.BackOffice.Api.Common;
using AppointMan.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.BackOffice.Api.Controllers.Owner
{
    /// <summary>
    /// Isletme sahibinin randevu yonetimi. Route seviyesinde BusinessOwner
    /// rolune sinirlidir; isletme kapsami handler'larda dogrulanir.
    /// Iptal domain kurallarini mevcut musteri/isletme akisindan alir.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "BusinessOwner")]
    [Route("api/v1/owner")]
    public class OwnerAppointmentsController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public OwnerAppointmentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Sahibin isletmesinin randevulari; durum ve tarih filtreleriyle.</summary>
        [HttpGet("businesses/{businessId:guid}/appointments")]
        public async Task<IActionResult> GetMyAppointments(
            Guid businessId,
            [FromQuery] EnumAppointmentStatus? status,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyAppointmentsQuery
            {
                BusinessId = businessId,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page ?? 1,
                PageSize = pageSize ?? 50
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkList(result.Value!.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>
        /// Randevuyu isletme sahibi olarak iptal eder. Domain kurallari
        /// mevcut musteri/isletme iptal akisiyla ayni kaynaktan uygulanir.
        /// </summary>
        [HttpPost("businesses/{businessId:guid}/appointments/{appointmentId:guid}/cancel")]
        public async Task<IActionResult> CancelMyAppointment(
            Guid businessId,
            Guid appointmentId,
            [FromBody] OwnerCancelAppointmentRequest? body,
            CancellationToken cancellationToken)
        {
            // Gerekce bos gonderilirse handler dogrulama hatasi doner ve
            // buradan 400 BadRequest ile yansitilir.
            var result = await _mediator.Send(new CancelMyAppointmentCommand
            {
                BusinessId = businessId,
                AppointmentId = appointmentId,
                Reason = body?.Reason,
                CancellationReason = body?.CancellationReason
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkEnvelope(result.Value!);
        }

        /// <summary>Owner iptali istek govdesi. Reason zorunludur.</summary>
        public sealed class OwnerCancelAppointmentRequest
        {
            /// <summary>Sahibin iptal gerekcesi; zorunludur, denetim kaydina yazilir.</summary>
            public string? Reason { get; init; }

            /// <summary>Randevuya islenecek gerekce; bos ise Reason kullanilir.</summary>
            public string? CancellationReason { get; init; }
        }
    }
}
