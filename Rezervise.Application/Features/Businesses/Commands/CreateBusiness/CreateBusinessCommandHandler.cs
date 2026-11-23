using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Businesses.Commands.CreateBusiness
{
    public class CreateBusinessCommandHandler : IRequestHandler<CreateBusinessCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateBusinessCommandHandler(
            IRepository<Business> businessRepository,
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(CreateBusinessCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 0. İşletme yalnızca kendisi veya yönetici adına oluşturulabilir.
                //    Sahip ID'si gövdeden değil, JWT kimliğinden doğrulanır.
                if (!CallerAuthorization.CanActAsUser(_currentUser, request.OwnerId))
                    return Result<Guid>.Failure(
                        Error.Unauthorized("Başka kullanıcı adına işletme oluşturma yetkiniz yok."));

                // 1. Kullanıcı kontrolü
                var user = await _userRepository.GetByIdAsync(request.OwnerId, cancellationToken);
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanıcı", request.OwnerId.ToString()));

                // 2. Aynı isimde işletme var mi?
                var existingBusinesses = await _businessRepository.FindAsync(
                    b => b.Name == request.Name && !b.IsDeleted,
                    cancellationToken);

                if (existingBusinesses.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu isimde bir işletme zaten mevcut."));

                // 3. Domain entity oluştur
                var business = new Business(
                    request.Name,
                    request.BusinessType,
                    request.Address,
                    request.PhoneNumber,
                    request.Email,
                    request.OwnerId);

                // 4. Opsiyonel alanları doldur
                if (!string.IsNullOrEmpty(request.Description))
                    business.SetDescription(request.Description);

                if (!string.IsNullOrEmpty(request.WebsiteUrl))
                    business.UpdateContactInfo(request.PhoneNumber, request.Email, request.WebsiteUrl);

                if (!string.IsNullOrEmpty(request.LogoUrl))
                    business.UpdateLogo(request.LogoUrl);

                // 5. Veritabanına kaydet
                await _businessRepository.AddAsync(business, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(business.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("İşletme oluşturulurken bir hata oluştu.", ex.Message));
            }
        }
    }
}