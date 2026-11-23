using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Businesses.Commands.DeactivateBusiness
{
    public class DeactivateBusinessCommandHandler : IRequestHandler<DeactivateBusinessCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public DeactivateBusinessCommandHandler(
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(DeactivateBusinessCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü - JWT'den çözülen işletme sahibi veya yönetici mi?
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu işletmeyi pasif etme yetkiniz yok."));

                // 3. Zaten pasif mi?
                if (!business.IsActive)
                    return Result<Guid>.Failure(Error.Conflict("İşletme zaten pasif."));

                // 4. Pasif et
                // Denetim kaydı da gövde değil, JWT kimliğinden yazılır.
                business.Deactivate(_currentUser.UserId?.ToString() ?? "System", request.Reason);

                // 5. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(business.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("İşletme pasif edilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}