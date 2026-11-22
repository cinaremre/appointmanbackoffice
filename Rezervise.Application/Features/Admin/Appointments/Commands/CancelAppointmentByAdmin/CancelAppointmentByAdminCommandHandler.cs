using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Appointments.Commands.CancelAppointment;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Appointments.Commands.CancelAppointmentByAdmin
{
    /// <summary>
    /// Yonetici randevu iptali (dokuman Modul 12.2). Bu handler kendi iptal
    /// algoritmasini yazmaz: randevuyu yukler, gerekceyi dogrular ve islemi
    /// mevcut <see cref="CancelAppointmentCommand"/> ile IMediator uzerinden
    /// gercek handler'a delege eder. Domain gecerli durum gecisi ve politika
    /// kontrolleri boylece tek kaynaktan yurutulur. Domain iptali basarisizsa
    /// ayni hata degismeden dondurulur; basari sonrasi denetim kaydi yazilir
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
                // 1. Yonetici kimligi JWT'den cozulur; iptal yetkisi policy
                //    seviyesinde (appointment.cancel) kontrol edilir.
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                var actorId = _currentUser.UserId.Value;
                var actorRole = _currentUser.Roles.FirstOrDefault() ?? "Unknown";

                // 2. Gerekce zorunludur: denetim kaydi gerekcesiz yapilamaz.
                var reason = request.Reason?.Trim();
                if (string.IsNullOrEmpty(reason))
                    return Result<Guid>.Failure(Error.Validation("Iptal gerekcesi zorunludur."));

                // 3. Randevuyu onceden yukle: varlik kontrolu ve denetim kaydi
                //    icin isletme kimligi buradan alinir.
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Randevu", request.AppointmentId));

                var oldStatus = appointment.Status;

                // 4. Domain iptali: mevcut CancelAppointmentCommand ile gercek
                //    handler'a delege edilir. State transition, politika ve
                //    yetki kontrolleri tek kaynaktan yurutulur (dokuman 12.2).
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

                // 5. Domain iptali basarisizsa ayni hatayi dondur; denetim kaydi
                //    yazilmaz.
                if (cancelResult.IsFailure)
                    return cancelResult;

                // 6. Basari sonrasi denetim kaydi: durum degisikligi old/new
                //    degerleriyle kayda gecer.
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
                    "Randevu yonetici iptali sirasinda bir hata olustu.", ex.Message));
            }
        }
    }
}
