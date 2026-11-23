using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.ValueObjects;
using MediatR;

namespace Rezervise.Application.Features.Employees.Commands.CreateEmployee
{
    public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
    {
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateEmployeeCommandHandler(
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

        public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü: gövde OwnerId'si değil, JWT kimliği esas alınır.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu işletmeye çalışan ekleme yetkiniz yok."));

                // 3. Aynı isimde çalışan var mi?
                var existingEmployees = await _employeeRepository.FindAsync(
                    e => e.BusinessId == request.BusinessId &&
                         e.FirstName == request.FirstName &&
                         e.LastName == request.LastName &&
                         !e.IsDeleted,
                    cancellationToken);

                if (existingEmployees.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu isimde bir çalışan zaten mevcut."));

                // 4. Telefon numarası oluştur
                var phoneNumber = new PhoneNumber(request.PhoneNumber);

                // 5. Çalışan oluştur
                var employee = new Employee(
                    request.FirstName,
                    request.LastName,
                    phoneNumber,
                    request.BusinessId);

                if (!string.IsNullOrEmpty(request.Title))
                    employee.SetTitle(request.Title);

                if (!string.IsNullOrEmpty(request.Bio))
                    employee.SetBio(request.Bio);

                if (!string.IsNullOrEmpty(request.ProfilePictureUrl))
                    employee.SetProfilePicture(request.ProfilePictureUrl);

                if (!string.IsNullOrEmpty(request.Email))
                {
                    var email = new Email(request.Email);
                    employee.SetEmail(email);
                }

                employee.SetDisplayOrder(request.DisplayOrder);

                // 6. Kaydet
                await _employeeRepository.AddAsync(employee, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(employee.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Çalışan oluşturulurken bir hata oluştu.", ex.Message));
            }
        }
    }
}