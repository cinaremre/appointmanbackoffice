using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Common.Errors;
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

        public CancelAppointmentCommandHandler(
            IRepository<Appointment> appointmentRepository,
            IDateTimeService dateTimeService,
            IUnitOfWork unitOfWork,
            AppointManMetrics metrics)
        {
            _appointmentRepository = appointmentRepository;
            _dateTimeService = dateTimeService;
            _unitOfWork = unitOfWork;
            _metrics = metrics;
        }

        public async Task<Result<Guid>> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Randevu kontrolu
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                // 2. Yetki kontrolu - Kullanici randevunun sahibi mi?
                if (appointment.UserId != request.UserId)
                    return Result<Guid>.Failure(Error.Unauthorized("Bu randevuyu iptal etme yetkiniz yok."));

                // 3. Domain kurallari ile iptal et.
                //    Ayni referans zamani kullanildigi icin es zamanli iki iptal
                //    isteginden ikincisi rowversion cakismasi ile reddedilir.
                appointment.Cancel(request.Reason, request.CancelledBy ?? "User", _dateTimeService.UtcNow);

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
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Randevu iptal edilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
