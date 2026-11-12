using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Appointments.Commands.CompleteAppointment
{
    public class CompleteAppointmentCommandHandler : IRequestHandler<CompleteAppointmentCommand, Result<Guid>>
    {
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppointManMetrics _metrics;

        public CompleteAppointmentCommandHandler(
            IRepository<Appointment> appointmentRepository,
            IUnitOfWork unitOfWork,
            AppointManMetrics metrics)
        {
            _appointmentRepository = appointmentRepository;
            _unitOfWork = unitOfWork;
            _metrics = metrics;
        }

        public async Task<Result<Guid>> Handle(CompleteAppointmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Randevu kontrolu
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                // 2. Yetki kontrolu - Isletme sahibi mi?
                if (appointment.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.Unauthorized("Bu randevuyu tamamlama yetkiniz yok."));

                // 3. Domain kurallari ile tamamla
                appointment.Complete(request.CompletedBy ?? "Business");

                // 4. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _metrics.RecordAppointmentCompleted();

                return Result<Guid>.Success(appointment.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Randevu tamamlanirken bir hata olustu.", ex.Message));
            }
        }
    }
}