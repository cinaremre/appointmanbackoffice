using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Businesses.Commands.CreateBusiness
{
    public class CreateBusinessCommandHandler : IRequestHandler<CreateBusinessCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateBusinessCommandHandler(
            IRepository<Business> businessRepository,
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork)
        {
            _businessRepository = businessRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(CreateBusinessCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kullanici kontrolu
                var user = await _userRepository.GetByIdAsync(request.OwnerId, cancellationToken);
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanici", request.OwnerId.ToString()));

                // 2. Ayni isimde isletme var mi?
                var existingBusinesses = await _businessRepository.FindAsync(
                    b => b.Name == request.Name && !b.IsDeleted,
                    cancellationToken);

                if (existingBusinesses.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu isimde bir isletme zaten mevcut."));

                // 3. Domain entity olustur
                var business = new Business(
                    request.Name,
                    request.BusinessType,
                    request.Address,
                    request.PhoneNumber,
                    request.Email,
                    request.OwnerId);

                // 4. Opsiyonel alanlari doldur
                if (!string.IsNullOrEmpty(request.Description))
                    business.SetDescription(request.Description);

                if (!string.IsNullOrEmpty(request.WebsiteUrl))
                    business.UpdateContactInfo(request.PhoneNumber, request.Email, request.WebsiteUrl);

                if (!string.IsNullOrEmpty(request.LogoUrl))
                    business.UpdateLogo(request.LogoUrl);

                // 5. Veritabanina kaydet
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
                return Result<Guid>.Failure(Error.Internal("Isletme olusturulurken bir hata olustu.", ex.Message));
            }
        }
    }
}