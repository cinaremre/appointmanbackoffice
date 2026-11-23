using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.ValueObjects;
using MediatR;

namespace Rezervise.Application.Features.Employees.Commands.UpdateEmployee
{
    public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Result<Guid>>
    {
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public UpdateEmployeeCommandHandler(
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

        public async Task<Result<Guid>> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü: gövde OwnerId'si değil, JWT kimliği esas alınır.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu çalışanı güncelleme yetkiniz yok."));

                // 3. Çalışan kontrolü
                var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
                if (employee is null)
                    return Result<Guid>.Failure(Error.NotFound("Çalışan", request.EmployeeId.ToString()));

                // 4. Çalışan işletmeye ait mi?
                if (employee.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.BusinessRule("Çalışan bu işletmeye ait değil."));

                // 5. Aynı isimde başka çalışan var mi?
                var existingEmployees = await _employeeRepository.FindAsync(
                    e => e.BusinessId == request.BusinessId &&
                         e.FirstName == request.FirstName &&
                         e.LastName == request.LastName &&
                         e.Id != request.EmployeeId &&
                         !e.IsDeleted,
                    cancellationToken);

                if (existingEmployees.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu isimde bir çalışan zaten mevcut."));

                // 6. Profil güncelle
                employee.UpdateProfile(
                    request.FirstName,
                    request.LastName,
                    request.Title,
                    request.Bio,
                    request.ProfilePictureUrl);

                // 7. Telefon güncelle
                var phoneNumber = new PhoneNumber(request.PhoneNumber);
                employee.SetPhoneNumber(phoneNumber);

                // 8. Email güncelle
                if (!string.IsNullOrEmpty(request.Email))
                {
                    var email = new Email(request.Email);
                    employee.SetEmail(email);
                }
                else
                {
                    employee.SetEmail(null);
                }

                // 9. Sıralama güncelle
                employee.SetDisplayOrder(request.DisplayOrder);

                // 10. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(employee.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Çalışan güncellenirken bir hata oluştu.", ex.Message));
            }
        }
    }
}