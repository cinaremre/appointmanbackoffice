using AppointMan.Application.Common.Errors;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Common.Security;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using AppointMan.Domain.ValueObjects;
using MediatR;
using BCryptNet = BCrypt.Net.BCrypt;

namespace AppointMan.Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterUserCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Email kontrolu
                var email = new Email(request.Email);
                var existingUserByEmail = await _userRepository.FindAsync(
                    u => u.Email == email && !u.IsDeleted,
                    cancellationToken);

                if (existingUserByEmail.Any())
                    return Result<Guid>.Failure(Error.Conflict("User.EmailTaken"));

                // 2. Telefon kontrolu
                var phone = new PhoneNumber(request.PhoneNumber);
                var existingUserByPhone = await _userRepository.FindAsync(
                    u => u.PhoneNumber == phone && !u.IsDeleted,
                    cancellationToken);

                if (existingUserByPhone.Any())
                    return Result<Guid>.Failure(Error.Conflict("User.PhoneTaken"));

                // 3. Yetkilendirme: istemci yalnizca self-assignable rolleri talep edebilir.
                //    Admin / SuperAdmin / Moderator gibi roller yalnizca yonetici tarafindan atanir.
                if (!SelfAssignableRoles.IsSelfAssignable(request.Role))
                    return Result<Guid>.Failure(Error.Forbidden("Validation.RoleNotSelfAssignable"));

                // 4. Domain entity olustur
                var user = new User(
                    request.FirstName,
                    request.LastName,
                    email,
                    phone,
                    request.Role);

                // 4. Sifre hashle (BCrypt)
                var passwordHash = BCryptNet.HashPassword(request.Password);
                user.SetPasswordHash(passwordHash);

                if (request.DateOfBirth.HasValue)
                {
                    user.SetDateOfBirth(request.DateOfBirth);
                }

                // 5. Konum bilgisi (il / ilce) - aramalarda varsayilan konum olarak kullanilir
                if (request.CityId.HasValue || request.DistrictId.HasValue)
                {
                    user.UpdateLocation(request.CityId, request.DistrictId);
                }

                // 6. Dil tercihi - bos gelirse domain varsayilani ("tr") korunur
                if (!string.IsNullOrWhiteSpace(request.LanguageCode))
                {
                    user.SetLanguageCode(request.LanguageCode);
                }

                // 7. Kaydet
                await _userRepository.AddAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(user.Id);
            }
            catch (DuplicateKeyException)
            {
                // Es zamanli iki kayit denemesinde ikincisi unique index'e takilir.
                return Result<Guid>.Failure(ConcurrencyErrors.DuplicateRegistration());
            }
            catch (ConcurrencyConflictException)
            {
                return Result<Guid>.Failure(ConcurrencyErrors.ConcurrencyConflict());
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Key));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("User.RegisterFailed", ex.Message));
            }
        }
    }
}