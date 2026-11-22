using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Businesses.Commands.UpdateBusiness
{
    public class UpdateBusinessCommandHandler : IRequestHandler<UpdateBusinessCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public UpdateBusinessCommandHandler(
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(UpdateBusinessCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu - JWT'den cozulen isletme sahibi veya yonetici mi?
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu isletmeyi guncelleme yetkiniz yok."));

                // 3. Ayni isimde baska isletme var mi?
                var existingBusinesses = await _businessRepository.FindAsync(
                    b => b.Name == request.Name && b.Id != request.BusinessId && !b.IsDeleted,
                    cancellationToken);

                if (existingBusinesses.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu isimde bir isletme zaten mevcut."));

                // 4. Bilgileri guncelle
                business.SetName(request.Name);
                business.SetDescription(request.Description);
                business.UpdateContactInfo(request.PhoneNumber, request.Email, request.WebsiteUrl);
                business.UpdateAddress(request.Address);

                if (!string.IsNullOrEmpty(request.LogoUrl))
                    business.UpdateLogo(request.LogoUrl);

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
                return Result<Guid>.Failure(Error.Internal("Isletme guncellenirken bir hata olustu.", ex.Message));
            }
        }
    }
}