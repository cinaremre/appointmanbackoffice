using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Appointments.Commands.CancelAppointment;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Appointments.Commands.CancelMyAppointment
{
    /// <summary>
    /// İşletme sahibi randevu iptali. Bu handler kendi iptal algorıtmasını
    /// yazmaz: sahiplik ve işletme uyumluluğunu doğrular, ardından işlemi
    /// mevcut <see cref="CancelAppointmentCommand"/> ile IMediator üzerinden
    /// gerçek handler'a delege eder. Domain geçerli durum geçişi ve politika
    /// kontrolleri böylece tek kaynaktan yürütülür. Başarı sonrası denetim
    /// kaydı yazılır (Action=OwnerCancelAppointment).
    /// </summary>
    public class CancelMyAppointmentCommandHandler : IRequestHandler<CancelMyAppointmentCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public CancelMyAppointmentCommandHandler(
            IRepository<Business> businessRepository,
            IRepository<Appointment> appointmentRepository,
            IMediator mediator,
            ICurrentUserService currentUser,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _businessRepository = businessRepository;
            _appointmentRepository = appointmentRepository;
            _mediator = mediator;
            _currentUser = currentUser;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(
            CancelMyAppointmentCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahip kimliği JWT'den çözülür.
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                var actorId = _currentUser.UserId.Value;
                var actorRole = _currentUser.Roles.FirstOrDefault() ?? "BusinessOwner";

                // 2. Gerekçe zorunludur: denetim kaydı gerekçesiz yapılamaz.
                var reason = request.Reason?.Trim();
                if (string.IsNullOrEmpty(reason))
                    return Result<Guid>.Failure(Error.Validation("İptal gerekçesi zorunludur."));

                // 3. İşletme sahipliği: owner kapsamının tek kapısı.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<Guid>.Failure(owned.Error!);

                // 4. Randevuyu önceden yükle: varlık + işletme uyumluluğu.
                var appointment = await _appointmentRepository.GetByIdAsync(
                    request.AppointmentId, cancellationToken);

                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Randevu", request.AppointmentId));

                // 5. Randevu istenen işletmeye ait olmalı; sahiplik zaten
                //    işletmeden geçtiği için bu kontrol yeterlidir.
                if (appointment.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(
                        Error.Forbidden("Randevu bu işletmeye ait değil."));

                var oldStatus = appointment.Status;

                // 6. Domain iptali: mevcut CancelAppointmentCommand ile gerçek
                //    handler'a delege edilir. State transition, politika ve
                //    yetki kontrolleri tek kaynaktan yürütülür.
                var domainReason = string.IsNullOrWhiteSpace(request.CancellationReason)
                    ? reason
                    : request.CancellationReason.Trim();

                var cancelResult = await _mediator.Send(
                    new CancelAppointmentCommand
                    {
                        AppointmentId = request.AppointmentId,
                        UserId = appointment.UserId,
                        Reason = domainReason,
                        CancelledBy = $"Owner:{actorId}"
                    },
                    cancellationToken);

                // 7. Domain iptali başarısızsa aynı hatayı döndür; denetim
                //    kaydı yazılmaz.
                if (cancelResult.IsFailure)
                    return cancelResult;

                // 8. Başarı sonrası denetim kaydı: durum değişikliği old/new
                //    değerleriyle kayda geçer.
                await _auditSink.RecordAsync(
                    actorId,
                    actorRole,
                    appointment.BusinessId,
                    "Appointment",
                    appointment.Id.ToString(),
                    "OwnerCancelAppointment",
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
                    "Randevu işletme sahibi iptali sırasında bir hata oluştu.", ex.Message));
            }
        }
    }
}
