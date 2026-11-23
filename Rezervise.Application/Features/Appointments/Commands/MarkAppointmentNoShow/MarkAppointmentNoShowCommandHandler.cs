using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Errors;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Appointments.Commands.MarkAppointmentNoShow
{
    /// <summary>
    /// Randevuyu "gelmedi" olarak işaretler.
    /// Yetki: işletme sahibi veya randevunun atanmış çalışanı. Müşterinin kendisi
    /// bu işlemi yapamaz; no-show kararı hizmeti veren tarafa aittir.
    /// </summary>
    public class MarkAppointmentNoShowCommandHandler
        : IRequestHandler<MarkAppointmentNoShowCommand, Result<Guid>>
    {
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeService _dateTimeService;
        private readonly ICurrentUserService _currentUser;

        public MarkAppointmentNoShowCommandHandler(
            IRepository<Appointment> appointmentRepository,
            IUnitOfWork unitOfWork,
            IDateTimeService dateTimeService,
            ICurrentUserService currentUser)
        {
            _appointmentRepository = appointmentRepository;
            _unitOfWork = unitOfWork;
            _dateTimeService = dateTimeService;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(
            MarkAppointmentNoShowCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                var appointment = await _appointmentRepository.GetByIdWithIncludesAsync(
                    request.AppointmentId,
                    cancellationToken,
                    a => a.Business,
                    a => a.Employee);

                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                // Yetki: işletme sahibi veya randevuya atanmış çalışan.
                var isBusinessOwner = appointment.Business is not null &&
                                      CallerAuthorization.CanActForBusiness(
                                          _currentUser, appointment.Business.OwnerId);

                var isAssignedEmployee = appointment.Employee is not null &&
                                         appointment.Employee.IsLinkedToUser(_currentUser.UserId.Value);

                if (!isBusinessOwner && !isAssignedEmployee)
                    return Result<Guid>.Failure(
                        Error.Forbidden("Bu randevuyu gelmedi olarak işaretleme yetkiniz yok."));

                // Domain çağrılmadan önce on kontrol: generic domain mesajına düşmeyi engeller.
                // Tek referans zamanı kullanılır; status matrisi domain'de tek kaynaktır.
                var now = _dateTimeService.UtcNow;

                if (!appointment.CanTransitionTo(EnumAppointmentStatus.NoShow, now))
                    return Result<Guid>.Failure(Error.BusinessRule(
                        "Sadece geçmiş, bekleyen veya onaylanmış randevular gelmedi olarak işaretlenebilir."));

                appointment.MarkAsNoShow(request.ChangedBy ?? "User", now);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

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
                return Result<Guid>.Failure(
                    Error.Internal("Randevu gelmedi olarak işaretlenirken bir hata oluştu."));
            }
        }
    }
}
