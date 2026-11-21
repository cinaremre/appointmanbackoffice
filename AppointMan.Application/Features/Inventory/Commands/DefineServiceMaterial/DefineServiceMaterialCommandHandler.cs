using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Inventory.Commands.DefineServiceMaterial
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
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu isletmeye recete tanimlama yetkiniz yok."));

                // 3. Hizmet kontrolu: hizmet isletmeye ait olmali.
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
                if (service is null || service.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                // 4. Urun kontrolu: urun isletmeye ait olmali.
                var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null || product.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.NotFound("Urun", request.ProductId.ToString()));

                // 5. Ayni (ServiceId, ProductId) cifti daha once tanimlanmis mi?
                var existing = await _serviceMaterialRepository.FindAsync(
                    sm => sm.BusinessId == request.BusinessId
                          && sm.ServiceId == request.ServiceId
                          && sm.ProductId == request.ProductId
                          && !sm.IsDeleted,
                    cancellationToken);

                if (existing.Any())
                    return Result<Guid>.Failure(Error.Conflict(
                        "Bu hizmet icin bu urun zaten recetede tanimli."));

                // 6. Recete kaydini olustur
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
                // Es zamanli iki tanimlama denemesi: unique index ikincisini reddeder.
                return Result<Guid>.Failure(
                    Error.Conflict("Bu hizmet icin bu urun zaten recetede tanimli."));
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Hizmet recetesi tanimlanirken bir hata olustu.", ex.Message));
            }
        }
    }
}
