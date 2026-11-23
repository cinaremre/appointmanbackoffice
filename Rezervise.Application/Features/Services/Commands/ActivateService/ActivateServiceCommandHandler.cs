using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Services.Commands.ActivateService
{
    public class ActivateServiceCommandHandler : IRequestHandler<ActivateServiceCommand, Result<Guid>>
    {
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public ActivateServiceCommandHandler(
            IRepository<Service> serviceRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _serviceRepository = serviceRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(ActivateServiceCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü - JWT'den çözülen işletme sahibi veya yönetici mi?
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu hizmeti aktif etme yetkiniz yok."));

                // 3. Hizmet kontrolü
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
                if (service is null)
                    return Result<Guid>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                // 4. Hizmet işletmeye ait mi?
                if (service.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.BusinessRule("Hizmet bu işletmeye ait değil."));

                // 5. Zaten aktif mi?
                if (service.IsActive)
                    return Result<Guid>.Failure(Error.Conflict("Hizmet zaten aktif."));

                // 6. Aktif et
                // Denetim kaydı da gövde değil, JWT kimliğinden yazılır.
                service.Activate(_currentUser.UserId?.ToString() ?? "System");

                // 7. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(service.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Hizmet aktif edilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}