using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Appointments.Commands.CancelAppointment;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Appointments.Commands.CancelAppointmentByAdmin
{
    /// <summary>
    /// Yönetici randevu iptali (doküman Modül 12.2). Bu handler kendi iptal
    /// algorıtmasını yazmaz: randevuyu yükler, gerekçeyi doğrular ve işlemi
    /// mevcut <see cref="CancelAppointmentCommand"/> ile IMediator üzerinden
    /// gerçek handler'a delege eder. Domain geçerli durum geçişi ve politika
    /// kontrolleri böylece tek kaynaktan yürütülür. Domain iptali başarısızsa
    /// aynı hata değişmeden dondurulur; başarı sonrası denetim kaydı yazılır
    /// (Action=CancelAppointment).
    /// </summary>
    public class CancelAppointmentByAdminCommandHandler : IRequestHandler<CancelAppointmentByAdminCommand, Result<Guid>>
    {
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public CancelAppointmentByAdminCommandHandler(
            IRepository<Appointment> appointmentRepository,
            IMediator mediator,
            ICurrentUserService currentUser,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _appointmentRepository = appointmentRepository;
            _mediator = mediator;
            _currentUser = currentUser;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CancelAppointmentByAdminCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yönetici kimliği JWT'den çözülür; iptal yetkisi policy
                //    seviyesinde (appointment.cancel) kontrol edilir.
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                var actorId = _currentUser.UserId.Value;
                var actorRole = _currentUser.Roles.FirstOrDefault() ?? "Unknown";

                // 2. Gerekçe zorunludur: denetim kaydı gerekçesiz yapılamaz.
                var reason = request.Reason?.Trim();
                if (string.IsNullOrEmpty(reason))
                    return Result<Guid>.Failure(Error.Validation("İptal gerekçesi zorunludur."));

                // 3. Randevuyu önceden yükle: varlık kontrolü ve denetim kaydı
                //    için işletme kimliği buradan alınır.
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Randevu", request.AppointmentId));

                var oldStatus = appointment.Status;

                // 4. Domain iptali: mevcut CancelAppointmentCommand ile gerçek
                //    handler'a delege edilir. State transition, politika ve
                //    yetki kontrolleri tek kaynaktan yürütülür (doküman 12.2).
                var domainReason = string.IsNullOrWhiteSpace(request.CancellationReason)
                    ? reason
                    : request.CancellationReason.Trim();

                var cancelResult = await _mediator.Send(
                    new CancelAppointmentCommand
                    {
                        AppointmentId = request.AppointmentId,
                        UserId = appointment.UserId,
                        Reason = domainReason,
                        CancelledBy = $"Admin:{actorId}"
                    },
                    cancellationToken);

                // 5. Domain iptali başarısızsa aynı hatayı döndür; denetim kaydı
                //    yazılmaz.
                if (cancelResult.IsFailure)
                    return cancelResult;

                // 6. Başarı sonrası denetim kaydı: durum değişikliği old/new
                //    değerleriyle kayda geçer.
                await _auditSink.RecordAsync(
                    actorId,
                    actorRole,
                    appointment.BusinessId,
                    "Appointment",
                    appointment.Id.ToString(),
                    "CancelAppointment",
                    oldValues: $"{{\"status\":\"{oldStatus}\"}}",
                    newValues: $"{{\"status\":\"{appointment.Status}\"}}",
                    reason: reason,
                    correlationId: _auditContext.CorrelationId,
                    ipAddress: _auditContext.IpAddress,
                    cancellationToken: cancellationToken);

                return cancelResult;
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal(
                    "Randevu yönetici iptali sırasında bir hata oluştu.", ex.Message));
            }
        }
    }
}
