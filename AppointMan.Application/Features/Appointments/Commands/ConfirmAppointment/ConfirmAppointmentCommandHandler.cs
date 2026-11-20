using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Common.Errors;
using AppointMan.Application.Common.Interfaces;
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
        private readonly IRepository<Business> _businessRepository;
        private readonly IDateTimeService _dateTimeService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppointManMetrics _metrics;
        private readonly ICurrentUserService _currentUser;

        public ConfirmAppointmentCommandHandler(
            IRepository<Appointment> appointmentRepository,
            IRepository<Business> businessRepository,
            IDateTimeService dateTimeService,
            IUnitOfWork unitOfWork,
            AppointManMetrics metrics,
            ICurrentUserService currentUser)
        {
            _appointmentRepository = appointmentRepository;
            _businessRepository = businessRepository;
            _dateTimeService = dateTimeService;
            _unitOfWork = unitOfWork;
            _metrics = metrics;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(ConfirmAppointmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                // 1. Randevu kontrolu
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                // 2. Isletme, govdedeki BusinessId'ye degil randevunun kendisinden yuklenir.
                var business = await _businessRepository.GetByIdAsync(appointment.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", appointment.BusinessId.ToString()));

                // 3. Yetki kontrolu - isletme sahibi veya yonetici.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu randevuyu onaylama yetkiniz yok."));

                // 4. Domain kurallari ile onayla.
                //    Es zamanli iki onay isteginden ikincisi rowversion cakismasi ile reddedilir.
                appointment.Confirm(request.ConfirmedBy ?? "Business", _dateTimeService.UtcNow);

                // 5. Kaydet
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
            catch (Exception)
            {
                return Result<Guid>.Failure(Error.Internal("Randevu onaylanirken bir hata olustu."));
            }
        }
    }
}
