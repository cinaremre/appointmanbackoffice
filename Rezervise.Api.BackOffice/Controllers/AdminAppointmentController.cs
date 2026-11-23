using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Appointments.Commands.CancelAppointmentByAdmin;
using Rezervise.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentConflicts;
using Rezervise.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentDetail;
using Rezervise.Application.Features.Admin.Appointments.Queries.GetAdminAppointments;
using Rezervise.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Randevu operasyonları (doküman Modül 12). Okuma AppointmentRead,
    /// yönetici iptali AppointmentCancel permission'ina bağlıdır.
    /// DOKUMAN 12.3: override (geçmiş/randevu kurallarını aşma) ilk sürümde
    /// yok; ayrı bir komutla eklenecek.
    /// </summary>
    [Route("api/v1/admin/appointments")]
    public sealed class AdminAppointmentController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminAppointmentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Randevuları filtreleyerek sayfalayarak listeler.</summary>
        [HttpGet]
        [Authorize(Policy = AdminPermissions.AppointmentRead)]
        [ProducesResponseType(typeof(AdminResponse<IReadOnlyList<AdminAppointmentListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetList([FromQuery] GetAdminAppointmentsQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return AdminErrorMapper.ToProblem(result.Error);

            return OkList(result.Value.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>Tek randevunun detayını kısa geçmişiyle getirir.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = AdminPermissions.AppointmentRead)]
        [ProducesResponseType(typeof(AdminResponse<AdminAppointmentDetailDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _mediator.Send(new GetAdminAppointmentDetailQuery { AppointmentId = id });
            if (result.IsFailure)
                return AdminErrorMapper.ToProblem(result.Error);

            return OkEnvelope(result.Value);
        }

        /// <summary>
        /// Randevuyu yönetici olarak iptal eder. Domain kuralları mevcut
        /// müşteri/işletme iptal akışıyla aynı kaynaktan uygulanır.
        /// </summary>
        [HttpPost("{id:guid}/cancel")]
        [Authorize(Policy = AdminPermissions.AppointmentCancel)]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] AdminCancelAppointmentRequest? body)
        {
            var result = await _mediator.Send(new CancelAppointmentByAdminCommand
            {
                AppointmentId = id,
                Reason = body?.Reason,
                CancellationReason = body?.CancellationReason
            });

            if (result.IsFailure)
                return AdminErrorMapper.ToProblem(result.Error);

            return OkEnvelope(new { id, status = "cancelled" });
        }

        /// <summary>
        /// Bir işletmenin belirli günündeki çalışan çakışmalarını listeler.
        /// DuplicateKey index'i çakışmayı zaten engeller; endpoint tespit
        /// amaçlıdır.
        /// </summary>
        [HttpGet("conflicts")]
        [Authorize(Policy = AdminPermissions.AppointmentRead)]
        [ProducesResponseType(typeof(AdminResponse<IReadOnlyList<AdminAppointmentConflictDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetConflicts([FromQuery] Guid? businessId, [FromQuery] DateTime? date)
        {
            // Parametreler zorunludur; eksikse istek doğrulaması geçersizdir.
            if (!businessId.HasValue || businessId.Value == Guid.Empty)
                return AdminErrorMapper.ToProblem(Error.Validation("businessId zorunludur."));

            if (!date.HasValue)
                return AdminErrorMapper.ToProblem(Error.Validation("date zorunludur."));

            var result = await _mediator.Send(new GetAdminAppointmentConflictsQuery
            {
                BusinessId = businessId.Value,
                Date = date.Value
            });

            if (result.IsFailure)
                return AdminErrorMapper.ToProblem(result.Error);

            return OkEnvelope(result.Value);
        }

        /// <summary>Yönetici iptali istek gövdesi. Reason zorunludur.</summary>
        public sealed class AdminCancelAppointmentRequest
        {
            /// <summary>Yöneticinin iptal gerekçesi; zorunludur.</summary>
            public string? Reason { get; set; }

            /// <summary>Randevuya işlenecek gerekçe; boş ise Reason kullanılır.</summary>
            public string? CancellationReason { get; set; }
        }
    }
}
