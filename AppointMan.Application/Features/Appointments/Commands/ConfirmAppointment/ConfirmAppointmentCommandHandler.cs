using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Common.Errors;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Appointments.Commands.ConfirmAppointment
{
    public class ConfirmAppointmentCommandHandler : IRequestHandler<ConfirmAppointmentCommand, Result<Guid>>
    {
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IDateTimeService _dateTimeService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppointManMetrics _metrics;

        public ConfirmAppointmentCommandHandler(
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

        public async Task<Result<Guid>> Handle(ConfirmAppointmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Randevu kontrolu
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                // 2. Yetki kontrolu - Isletme sahibi mi?
                if (appointment.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.Unauthorized("Bu randevuyu onaylama yetkiniz yok."));

                // 3. Domain kurallari ile onayla.
                //    Es zamanli iki onay isteginden ikincisi rowversion cakismasi ile reddedilir.
                appointment.Confirm(request.ConfirmedBy ?? "Business", _dateTimeService.UtcNow);

                // 4. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _metrics.RecordAppointmentConfirmed();

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
                return Result<Guid>.Failure(Error.Internal("Randevu onaylanirken bir hata olustu.", ex.Message));
            }
        }
    }
}
