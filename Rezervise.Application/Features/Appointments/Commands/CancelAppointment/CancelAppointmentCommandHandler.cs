using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Diagnostics;
using Rezervise.Application.Common.Errors;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Appointments.Commands.CancelAppointment
{
    public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, Result<Guid>>
    {
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IDateTimeService _dateTimeService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly RezerviseMetrics _metrics;
        private readonly ICurrentUserService _currentUser;

        public CancelAppointmentCommandHandler(
            IRepository<Appointment> appointmentRepository,
            IDateTimeService dateTimeService,
            IUnitOfWork unitOfWork,
            RezerviseMetrics metrics,
            ICurrentUserService currentUser)
        {
            _appointmentRepository = appointmentRepository;
            _dateTimeService = dateTimeService;
            _unitOfWork = unitOfWork;
            _metrics = metrics;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                // 1. Randevu kontrolü (işletme/çalışan yetkisi için ilişkiler yüklenir).
                var appointment = await _appointmentRepository.GetByIdWithIncludesAsync(
                    request.AppointmentId,
                    cancellationToken,
                    a => a.Business,
                    a => a.Employee);

                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                // 2. Yetki kontrolü - randevu sahibi müşteri, işletme sahibi veya atanmış çalışan.
                var isBusinessSide =
                    (appointment.Business is not null &&
                     CallerAuthorization.CanActForBusiness(_currentUser, appointment.Business.OwnerId)) ||
                    (appointment.Employee is not null &&
                     appointment.Employee.IsLinkedToUser(_currentUser.UserId.Value));

                if (!isBusinessSide && !CallerAuthorization.CanActAsUser(_currentUser, appointment.UserId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu randevuyu iptal etme yetkiniz yok."));

                var now = _dateTimeService.UtcNow;

                // 2b. Müşteri son 2 saatte iptal edemez; işletme ve çalışan muaf.
                if (!isBusinessSide && appointment.IsWithinCustomerCutoff(now))
                    return Result<Guid>.Failure(Error.BusinessRule(
                        $"Randevuya {Appointment.CustomerChangeCutoffHours} saatten az kaldığı için iptal edilemez."));

                // 3. Domain kuralları ile iptal et.
                //    Aynı referans zamanı kullanıldığı için es zamanlı iki iptal
                //    isteğinden ikincisi rowversion çakışması ile reddedilir.
                appointment.Cancel(request.Reason, request.CancelledBy ?? "User", now);

                // 4. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _metrics.RecordAppointmentCancelled();

                return Result<Guid>.Success(appointment.Id);
            }
            catch (ConcurrencyConflictException)
            {
                return Result<Guid>.Failure(ConcurrencyErrors.ConcurrencyConflict());
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception)
            {
                return Result<Guid>.Failure(Error.Internal("Randevu iptal edilirken bir hata oluştu."));
            }
        }
    }
}
