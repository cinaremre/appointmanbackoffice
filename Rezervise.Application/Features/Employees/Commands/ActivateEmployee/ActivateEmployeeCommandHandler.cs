using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Employees.Commands.ActivateEmployee
{
    public class ActivateEmployeeCommandHandler : IRequestHandler<ActivateEmployeeCommand, Result<Guid>>
    {
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public ActivateEmployeeCommandHandler(
            IRepository<Employee> employeeRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _employeeRepository = employeeRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(ActivateEmployeeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu calisani aktif etme yetkiniz yok."));

                // 3. Calisan kontrolu
                var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
                if (employee is null)
                    return Result<Guid>.Failure(Error.NotFound("Calisan", request.EmployeeId.ToString()));

                // 4. Calisan isletmeye ait mi?
                if (employee.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan bu isletmeye ait degil."));

                // 5. Aktif et
                employee.Activate();

                // 6. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(employee.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Calisan aktif edilirken bir hata olustu.", ex.Message));
            }
        }
    }
}