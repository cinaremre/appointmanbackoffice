using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using AppointMan.Domain.ValueObjects;
using MediatR;

namespace AppointMan.Application.Features.Employees.Commands.UpdateEmployee
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
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu calisani guncelleme yetkiniz yok."));

                // 3. Calisan kontrolu
                var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
                if (employee is null)
                    return Result<Guid>.Failure(Error.NotFound("Calisan", request.EmployeeId.ToString()));

                // 4. Calisan isletmeye ait mi?
                if (employee.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.BusinessRule("Calisan bu isletmeye ait degil."));

                // 5. Ayni isimde baska calisan var mi?
                var existingEmployees = await _employeeRepository.FindAsync(
                    e => e.BusinessId == request.BusinessId &&
                         e.FirstName == request.FirstName &&
                         e.LastName == request.LastName &&
                         e.Id != request.EmployeeId &&
                         !e.IsDeleted,
                    cancellationToken);

                if (existingEmployees.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu isimde bir calisan zaten mevcut."));

                // 6. Profil guncelle
                employee.UpdateProfile(
                    request.FirstName,
                    request.LastName,
                    request.Title,
                    request.Bio,
                    request.ProfilePictureUrl);

                // 7. Telefon guncelle
                var phoneNumber = new PhoneNumber(request.PhoneNumber);
                employee.SetPhoneNumber(phoneNumber);

                // 8. Email guncelle
                if (!string.IsNullOrEmpty(request.Email))
                {
                    var email = new Email(request.Email);
                    employee.SetEmail(email);
                }
                else
                {
                    employee.SetEmail(null);
                }

                // 9. Siralama guncelle
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
                return Result<Guid>.Failure(Error.Internal("Calisan guncellenirken bir hata olustu.", ex.Message));
            }
        }
    }
}