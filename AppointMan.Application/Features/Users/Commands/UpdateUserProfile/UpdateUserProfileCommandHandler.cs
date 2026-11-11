using AppointMan.Application.Common.Errors;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Users.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Result<Guid>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateUserProfileCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kullanici kontrolu
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanici", request.UserId.ToString()));

                // 2. Domain kurallari ile guncelle
                user.UpdateProfile(
                    request.FirstName,
                    request.LastName,
                    request.DateOfBirth,
                    request.ProfilePictureUrl);

                // 3. Konum bilgisi (il / ilce) - sonraki aramalarda guncel konum kullanilir
                if (request.CityId.HasValue || request.DistrictId.HasValue)
                {
                    user.UpdateLocation(request.CityId, request.DistrictId);
                }

                // 4. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(user.Id);
            }
            catch (ConcurrencyConflictException)
            {
                // Rowversion uyusmazligi: baska bir islem ayni kullaniciyi guncelledi.
                return Result<Guid>.Failure(ConcurrencyErrors.ConcurrencyConflict());
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Profil guncellenirken bir hata olustu.", ex.Message));
            }
        }
    }
}