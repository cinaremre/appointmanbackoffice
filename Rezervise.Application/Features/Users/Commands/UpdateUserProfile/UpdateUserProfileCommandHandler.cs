using Rezervise.Application.Common.Errors;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.ValueObjects;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.UpdateUserProfile
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
                // 1. Kullanıcı kontrolü.
                //    Silinmiş (anonimleştirilmiş) hesap üzerinde profil güncellenemez;
                //    aksi halde silme işleminden sonra kişisel veri geri yazılabilirdi.
                var users = await _userRepository.FindAsync(
                    u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

                var user = users.FirstOrDefault();
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanıcı", request.UserId.ToString()));

                // 2. E-posta değişikliği (opsiyonel). Yalnızca gerçekten farklıysa
                //    yazılır; başka bir hesabın e-postası ise 409 döner.
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var newEmail = new Email(request.Email);

                    if (!string.Equals(user.Email, newEmail.Value, StringComparison.Ordinal))
                    {
                        var existingUserByEmail = await _userRepository.FindAsync(
                            u => u.Email == newEmail.Value && u.Id != user.Id && !u.IsDeleted,
                            cancellationToken);

                        if (existingUserByEmail.Any())
                            return Result<Guid>.Failure(Error.Conflict("User.EmailTaken"));

                        user.ChangeEmail(newEmail);
                    }
                }

                // 3. Domain kuralları ile güncelle
                user.UpdateProfile(
                    request.FirstName,
                    request.LastName,
                    request.DateOfBirth,
                    request.ProfilePictureUrl);

                // 4. Konum bilgisi (il / ilçe) - sonraki aramalarda güncel konum kullanılır
                if (request.CityId.HasValue || request.DistrictId.HasValue)
                {
                    user.UpdateLocation(request.CityId, request.DistrictId);
                }

                // 5. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(user.Id);
            }
            catch (DuplicateKeyException)
            {
                // Eş zamanlı iki istek aynı e-postayı yazmaya çalıştı.
                return Result<Guid>.Failure(Error.Conflict("User.EmailTaken"));
            }
            catch (ConcurrencyConflictException)
            {
                // Rowversion uyuşmazlığı: başka bir işlem aynı kullanıcıyı güncelledi.
                return Result<Guid>.Failure(ConcurrencyErrors.ConcurrencyConflict());
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Profil güncellenirken bir hata oluştu.", ex.Message));
            }
        }
    }
}