using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Common.Errors;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Appointments.Commands.CancelAppointment
{
    public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, Result<Guid>>
    {
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IDateTimeService _dateTimeService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppointManMetrics _metrics;
        private readonly ICurrentUserService _currentUser;

        public CancelAppointmentCommandHandler(
            IRepository<Appointment> appointmentRepository,
            IDateTimeService dateTimeService,
            IUnitOfWork unitOfWork,
            AppointManMetrics metrics,
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

                // 1. Randevu kontrolu (isletme/calisan yetkisi icin iliskiler yuklenir).
                var appointment = await _appointmentRepository.GetByIdWithIncludesAsync(
                    request.AppointmentId,
                    cancellationToken,
                    a => a.Business,
                    a => a.Employee);

                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                // 2. Yetki kontrolu - randevu sahibi musteri, isletme sahibi veya atanmis calisan.
                var isBusinessSide =
                    (appointment.Business is not null &&
                     CallerAuthorization.CanActForBusiness(_currentUser, appointment.Business.OwnerId)) ||
                    (appointment.Employee is not null &&
                     appointment.Employee.IsLinkedToUser(_currentUser.UserId.Value));

                if (!isBusinessSide && !CallerAuthorization.CanActAsUser(_currentUser, appointment.UserId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu randevuyu iptal etme yetkiniz yok."));

                var now = _dateTimeService.UtcNow;

                // 2b. Musteri son 2 saatte iptal edemez; isletme ve calisan muaf.
                if (!isBusinessSide && appointment.IsWithinCustomerCutoff(now))
                    return Result<Guid>.Failure(Error.BusinessRule(
                        $"Randevuya {Appointment.CustomerChangeCutoffHours} saatten az kaldigi icin iptal edilemez."));

                // 3. Domain kurallari ile iptal et.
                //    Ayni referans zamani kullanildigi icin es zamanli iki iptal
                //    isteginden ikincisi rowversion cakismasi ile reddedilir.
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
                return Result<Guid>.Failure(Error.Internal("Randevu iptal edilirken bir hata olustu."));
            }
        }
    }
}
