using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.ValueObjects;
using MediatR;

namespace Rezervise.Application.Features.Services.Commands.CreateService
{
    public class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, Result<Guid>>
    {
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<ServiceCategory> _serviceCategoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateServiceCommandHandler(
            IRepository<Service> serviceRepository,
            IRepository<Business> businessRepository,
            IRepository<ServiceCategory> serviceCategoryRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _serviceRepository = serviceRepository;
            _businessRepository = businessRepository;
            _serviceCategoryRepository = serviceCategoryRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu isletmeye hizmet ekleme yetkiniz yok."));

                // 3. Kategori kontrolu
                var serviceCategory = await _serviceCategoryRepository.GetByIdAsync(request.ServiceCategoryId, cancellationToken);
                if (serviceCategory is null)
                    return Result<Guid>.Failure(Error.NotFound("Kategori", request.ServiceCategoryId.ToString()));

                if (!serviceCategory.IsLeaf())
                    return Result<Guid>.Failure(Error.BusinessRule("Hizmet sadece leaf kategoriye atanabilir."));

                // 4. Kategori isletmede var mi?
                var rootCategory = serviceCategory.GetRootCategory();
                if (rootCategory is not null && !business.HasServiceCategory(rootCategory.Id))
                    return Result<Guid>.Failure(Error.BusinessRule("Bu kategori isletmeye ekli degil."));

                // 5. Ayni isimde hizmet var mi?
                var existingServices = await _serviceRepository.FindAsync(
                    s => s.Name == request.Name && s.BusinessId == request.BusinessId && !s.IsDeleted,
                    cancellationToken);

                if (existingServices.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu isimde bir hizmet zaten mevcut."));

                // 6. Hizmet olustur
                var money = new Money(request.Price, request.Currency);
                var service = new Service(
                    request.Name,
                    money,
                    request.DurationInMinutes,
                    request.ServiceCategoryId,
                    request.BusinessId);

                if (!string.IsNullOrEmpty(request.Description))
                    service.SetDescription(request.Description);

                // 7. Kaydet
                await _serviceRepository.AddAsync(service, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(service.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Hizmet olusturulurken bir hata olustu.", ex.Message));
            }
        }
    }
}