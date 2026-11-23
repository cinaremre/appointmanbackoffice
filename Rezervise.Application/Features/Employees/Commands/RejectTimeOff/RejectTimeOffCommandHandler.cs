using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Employees.Commands.RejectTimeOff
{
    /// <summary>
    /// Çalışan iznini reddeder (soft delete). Yetki: ilgili işletmenin sahibi veya yöneticisi.
    /// Aktör kimliği her zaman JWT'den gelir; gövde RequesterId'si yetki kaynağı değildir.
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

                // Gövde RequesterId'si yetki kaynağı değil; JWT ile tutarsızsa reddet.
                if (request.RequesterId != Guid.Empty && request.RequesterId != callerId)
                    return Result<Guid>.Failure(
                        Error.Unauthorized("Bu işlem için yetkisiz kullanıcı."));

                var timeOff = await _timeOffRepository.GetByIdAsync(
                    request.TimeOffId, cancellationToken);
                if (timeOff is null || timeOff.IsDeleted)
                    return Result<Guid>.Failure(
                        Error.NotFound("İzin", request.TimeOffId.ToString()));

                var employee = await _employeeRepository.GetByIdAsync(
                    timeOff.EmployeeId, cancellationToken);
                if (employee is null)
                    return Result<Guid>.Failure(
                        Error.NotFound("Çalışan", timeOff.EmployeeId.ToString()));

                var business = await _businessRepository.GetByIdAsync(
                    employee.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(
                        Error.NotFound("İşletme", employee.BusinessId.ToString()));

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
                    Error.Internal("İzin reddedilirken bir hata oluştu."));
            }
        }
    }
}
