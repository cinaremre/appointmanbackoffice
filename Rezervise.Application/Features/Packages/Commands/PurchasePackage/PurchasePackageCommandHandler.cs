using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Packages.Commands.PurchasePackage
{
    public class PurchasePackageCommandHandler : IRequestHandler<PurchasePackageCommand, Result<Guid>>
    {
        private readonly IRepository<ServicePackage> _servicePackageRepository;
        private readonly IRepository<CustomerPackage> _customerPackageRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTime;

        public PurchasePackageCommandHandler(
            IRepository<ServicePackage> servicePackageRepository,
            IRepository<CustomerPackage> customerPackageRepository,
            IRepository<Business> businessRepository,
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTime)
        {
            _servicePackageRepository = servicePackageRepository;
            _customerPackageRepository = customerPackageRepository;
            _businessRepository = businessRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Result<Guid>> Handle(PurchasePackageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Paket tanımı kontrolü
                var servicePackage = await _servicePackageRepository.GetByIdWithIncludesAsync(
                    request.ServicePackageId, cancellationToken, p => p.Items);

                if (servicePackage is null)
                    return Result<Guid>.Failure(Error.NotFound("Paket", request.ServicePackageId.ToString()));

                if (!servicePackage.IsActive)
                    return Result<Guid>.Failure(Error.Conflict("Paket su anda satışa kapalı."));

                // 2. İşletme ve yetki kontrolü: satışı işletme sahibi kaydeder.
                var business = await _businessRepository.GetByIdAsync(servicePackage.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", servicePackage.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Paket satışını yalnızca işletme sahibi kaydedebilir."));

                // 3. Müşteri kontrolü
                if (request.CustomerId == Guid.Empty)
                    return Result<Guid>.Failure(Error.Validation("Müşteri ID boş olamaz."));

                var customer = await _userRepository.GetByIdAsync(request.CustomerId, cancellationToken);
                if (customer is null)
                    return Result<Guid>.Failure(Error.NotFound("Müşteri", request.CustomerId.ToString()));

                // 4. Müşteri paketini oluştur (ExpiresAt = now + ValidityDays)
                var now = _dateTime.UtcNow;
                var customerPackage = new CustomerPackage(
                    servicePackage.BusinessId,
                    request.CustomerId,
                    servicePackage.Id,
                    servicePackage.ValidityDays,
                    now);

                // 5. Kaydet
                await _customerPackageRepository.AddAsync(customerPackage, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(customerPackage.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Paket satışı kaydedilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
