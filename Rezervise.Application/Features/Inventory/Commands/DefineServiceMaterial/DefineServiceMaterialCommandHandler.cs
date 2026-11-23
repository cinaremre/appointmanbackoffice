using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Inventory.Commands.DefineServiceMaterial
{
    public class DefineServiceMaterialCommandHandler : IRequestHandler<DefineServiceMaterialCommand, Result<Guid>>
    {
        private readonly IRepository<ServiceMaterial> _serviceMaterialRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public DefineServiceMaterialCommandHandler(
            IRepository<ServiceMaterial> serviceMaterialRepository,
            IRepository<Service> serviceRepository,
            IRepository<Product> productRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _serviceMaterialRepository = serviceMaterialRepository;
            _serviceRepository = serviceRepository;
            _productRepository = productRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(DefineServiceMaterialCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü: gövde OwnerId'si değil, JWT kimliği esas alınır.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu işletmeye reçete tanımlama yetkiniz yok."));

                // 3. Hizmet kontrolü: hizmet işletmeye ait olmalı.
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
                if (service is null || service.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                // 4. Ürün kontrolü: ürün işletmeye ait olmalı.
                var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null || product.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.NotFound("Ürün", request.ProductId.ToString()));

                // 5. Aynı (ServiceId, ProductId) çifti daha önce tanımlanmış mi?
                var existing = await _serviceMaterialRepository.FindAsync(
                    sm => sm.BusinessId == request.BusinessId
                          && sm.ServiceId == request.ServiceId
                          && sm.ProductId == request.ProductId
                          && !sm.IsDeleted,
                    cancellationToken);

                if (existing.Any())
                    return Result<Guid>.Failure(Error.Conflict(
                        "Bu hizmet için bu ürün zaten reçetede tanımlı."));

                // 6. Reçete kaydını oluştur
                var serviceMaterial = new ServiceMaterial(
                    request.BusinessId,
                    request.ServiceId,
                    request.ProductId,
                    request.QuantityPerService,
                    request.Unit);

                // 7. Kaydet
                await _serviceMaterialRepository.AddAsync(serviceMaterial, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(serviceMaterial.Id);
            }
            catch (DuplicateKeyException)
            {
                // Es zamanlı iki tanımlama denemesi: unique index ikincisini reddeder.
                return Result<Guid>.Failure(
                    Error.Conflict("Bu hizmet için bu ürün zaten reçetede tanımlı."));
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Hizmet reçetesi tanımlanırken bir hata oluştu.", ex.Message));
            }
        }
    }
}
