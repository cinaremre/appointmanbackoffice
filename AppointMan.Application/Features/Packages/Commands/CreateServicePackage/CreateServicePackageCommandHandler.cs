using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Packages.Commands.CreateServicePackage
{
    public class CreateServicePackageCommandHandler : IRequestHandler<CreateServicePackageCommand, Result<Guid>>
    {
        private readonly IRepository<ServicePackage> _servicePackageRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateServicePackageCommandHandler(
            IRepository<ServicePackage> servicePackageRepository,
            IRepository<Service> serviceRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _servicePackageRepository = servicePackageRepository;
            _serviceRepository = serviceRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(CreateServicePackageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu isletmeye paket tanimlama yetkiniz yok."));

                // 3. Kalemler dolu olmali
                if (request.Items is null || request.Items.Count == 0)
                    return Result<Guid>.Failure(Error.Validation("Paket en az bir hizmet kalemi icermelidir."));

                // 4. Ayni hizmet iki kez eklenemez
                var duplicateServiceId = request.Items
                    .GroupBy(i => i.ServiceId)
                    .FirstOrDefault(g => g.Count() > 1)?
                    .Key;

                if (duplicateServiceId.HasValue)
                    return Result<Guid>.Failure(Error.Validation("Ayni hizmet pakete iki kez eklenemez.", $"ServiceId: {duplicateServiceId}"));

                // 5. Hizmetler ayni isletmeye ait olmali
                foreach (var item in request.Items)
                {
                    var service = await _serviceRepository.GetByIdAsync(item.ServiceId, cancellationToken);
                    if (service is null)
                        return Result<Guid>.Failure(Error.NotFound("Hizmet", item.ServiceId.ToString()));

                    if (service.BusinessId != request.BusinessId)
                        return Result<Guid>.Failure(Error.Validation("Paket hizmetleri yalnizca ayni isletmenin hizmetleri olabilir.", $"ServiceId: {item.ServiceId}"));
                }

                // 6. Paketi kalemleriyle olustur
                var servicePackage = new ServicePackage(
                    request.Name,
                    request.Price,
                    request.ValidityDays,
                    request.BusinessId);

                foreach (var item in request.Items)
                {
                    servicePackage.AddItem(new ServicePackageItem(
                        servicePackage.Id,
                        item.ServiceId,
                        item.IncludedSessions));
                }

                // 7. Kaydet
                await _servicePackageRepository.AddAsync(servicePackage, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(servicePackage.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Paket olusturulurken bir hata olustu.", ex.Message));
            }
        }
    }
}
