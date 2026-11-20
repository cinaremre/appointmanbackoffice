using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Employees.Commands.RejectTimeOff
{
    /// <summary>
    /// Calisan iznini reddeder (soft delete). Yetki: ilgili isletmenin sahibi veya yoneticisi.
    /// Aktor kimligi her zaman JWT'den gelir; govde RequesterId'si yetki kaynagi degildir.
    /// </summary>
    public class RejectTimeOffCommandHandler : IRequestHandler<RejectTimeOffCommand, Result<Guid>>
    {
        private readonly IRepository<EmployeeTimeOff> _timeOffRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public RejectTimeOffCommandHandler(
            IRepository<EmployeeTimeOff> timeOffRepository,
            IRepository<Employee> employeeRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _timeOffRepository = timeOffRepository;
            _employeeRepository = employeeRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(
            RejectTimeOffCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                var callerId = _currentUser.UserId.Value;

                // Govde RequesterId'si yetki kaynagi degil; JWT ile tutarsizsa reddet.
                if (request.RequesterId != Guid.Empty && request.RequesterId != callerId)
                    return Result<Guid>.Failure(
                        Error.Unauthorized("Bu islem icin yetkisiz kullanici."));

                var timeOff = await _timeOffRepository.GetByIdAsync(
                    request.TimeOffId, cancellationToken);
                if (timeOff is null || timeOff.IsDeleted)
                    return Result<Guid>.Failure(
                        Error.NotFound("Izin", request.TimeOffId.ToString()));

                var employee = await _employeeRepository.GetByIdAsync(
                    timeOff.EmployeeId, cancellationToken);
                if (employee is null)
                    return Result<Guid>.Failure(
                        Error.NotFound("Calisan", timeOff.EmployeeId.ToString()));

                var business = await _businessRepository.GetByIdAsync(
                    employee.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(
                        Error.NotFound("Isletme", employee.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(
                        Error.Forbidden("Bu izni reddetme yetkiniz yok."));

                timeOff.Reject();

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(timeOff.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception)
            {
                return Result<Guid>.Failure(
                    Error.Internal("Izin reddedilirken bir hata olustu."));
            }
        }
    }
}
