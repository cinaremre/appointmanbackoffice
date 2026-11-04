using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Employees.Commands.DeactivateEmployee
{
    public class DeactivateEmployeeCommandHandler : IRequestHandler<DeactivateEmployeeCommand, Result<Guid>>
    {
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeactivateEmployeeCommandHandler(
            IRepository<Employee> employeeRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork)
        {
            _employeeRepository = employeeRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(DeactivateEmployeeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu
                if (business.OwnerId != request.OwnerId)
                    return Result<Guid>.Failure(Error.Unauthorized("Bu calisani pasif etme yetkiniz yok."));

                // 3. Calisan kontrolu
                var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
                if (employee is null)
                    return Result<Guid>.Failure(Error.NotFound("Calisan", request.EmployeeId.ToString()));

                // 4. Calisan isletmeye ait mi?
                if (employee.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan bu isletmeye ait degil."));

                // 5. Pasif et
                employee.Deactivate(request.Reason);

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
                return Result<Guid>.Failure(Error.Internal("Calisan pasif edilirken bir hata olustu.", ex.Message));
            }
        }
    }
}