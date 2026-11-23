using Rezervise.Application.Features.Owner.Appointments.Commands.CancelMyAppointment;
using Rezervise.Application.Features.Owner.Appointments.Queries.GetMyAppointments;
using Rezervise.BackOffice.Api.Common;
using Rezervise.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers.Owner
{
    /// <summary>
    /// İşletme sahibinin randevu yönetimi. Route seviyesinde BusinessOwner
    /// rolüne sınırlıdır; işletme kapsamı handler'larda doğrulanır.
    /// İptal domain kurallarını mevcut müşteri/işletme akışından alır.
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

        /// <summary>Sahibin işletmesinin randevuları; durum ve tarih filtreleriyle.</summary>
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
        /// Randevuyu işletme sahibi olarak iptal eder. Domain kuralları
        /// mevcut müşteri/işletme iptal akışıyla aynı kaynaktan uygulanır.
        /// </summary>
        [HttpPost("businesses/{businessId:guid}/appointments/{appointmentId:guid}/cancel")]
        public async Task<IActionResult> CancelMyAppointment(
            Guid businessId,
            Guid appointmentId,
            [FromBody] OwnerCancelAppointmentRequest? body,
            CancellationToken cancellationToken)
        {
            // Gerekçe boş gönderilirse handler doğrulama hatası döner ve
            // buradan 400 BadRequest ile yansıtılır.
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

        /// <summary>Owner iptali istek gövdesi. Reason zorunludur.</summary>
        public sealed class OwnerCancelAppointmentRequest
        {
            /// <summary>Sahibin iptal gerekçesi; zorunludur, denetim kaydına yazılır.</summary>
            public string? Reason { get; init; }

            /// <summary>Randevuya işlenecek gerekçe; boş ise Reason kullanılır.</summary>
            public string? CancellationReason { get; init; }
        }
    }
}
