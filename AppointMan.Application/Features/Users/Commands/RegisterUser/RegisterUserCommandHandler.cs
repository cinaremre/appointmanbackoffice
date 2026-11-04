using AppointMan.Application.Common.Results;
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
                    return Result<Guid>.Failure(Error.Conflict("Bu email adresi zaten kayitli."));

                // 2. Telefon kontrolu
                var phone = new PhoneNumber(request.PhoneNumber);
                var existingUserByPhone = await _userRepository.FindAsync(
                    u => u.PhoneNumber == phone && !u.IsDeleted,
                    cancellationToken);

                if (existingUserByPhone.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu telefon numarasi zaten kayitli."));

                // 3. Domain entity olustur
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

                // 5. Kaydet
                await _userRepository.AddAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(user.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Kullanici kaydedilirken bir hata olustu.", ex.Message));
            }
        }
    }
}