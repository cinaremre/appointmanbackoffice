using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Packages.Commands.CreateServicePackage
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
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü: JWT kimliği esas alınır.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu işletmeye paket tanımlama yetkiniz yok."));

                // 3. Kalemler dolu olmalı
                if (request.Items is null || request.Items.Count == 0)
                    return Result<Guid>.Failure(Error.Validation("Paket en az bir hizmet kalemi içermelidir."));

                // 4. Aynı hizmet iki kez eklenemez
                var duplicateServiceId = request.Items
                    .GroupBy(i => i.ServiceId)
                    .FirstOrDefault(g => g.Count() > 1)?
                    .Key;

                if (duplicateServiceId.HasValue)
                    return Result<Guid>.Failure(Error.Validation("Aynı hizmet pakete iki kez eklenemez.", $"ServiceId: {duplicateServiceId}"));

                // 5. Hizmetler aynı işletmeye ait olmalı
                foreach (var item in request.Items)
                {
                    var service = await _serviceRepository.GetByIdAsync(item.ServiceId, cancellationToken);
                    if (service is null)
                        return Result<Guid>.Failure(Error.NotFound("Hizmet", item.ServiceId.ToString()));

                    if (service.BusinessId != request.BusinessId)
                        return Result<Guid>.Failure(Error.Validation("Paket hizmetleri yalnızca aynı işletmenin hizmetleri olabilir.", $"ServiceId: {item.ServiceId}"));
                }

                // 6. Paketi kalemleriyle oluştur
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
                return Result<Guid>.Failure(Error.Internal("Paket oluşturulurken bir hata oluştu.", ex.Message));
            }
        }
    }
}
