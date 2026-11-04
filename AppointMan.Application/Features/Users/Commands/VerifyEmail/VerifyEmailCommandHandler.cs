using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Users.Commands.VerifyEmail
{
    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<Guid>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public VerifyEmailCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kullanici kontrolu
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanici", request.UserId.ToString()));

                // 2. Zaten dogrulanmis mi?
                if (user.IsEmailVerified)
                    return Result<Guid>.Failure(Error.Conflict("Email zaten dogrulanmis."));

                // 3. Dogrulama kodu kontrolu (gercek hayatta cache veya DB'de tutulur)
                // Burada ornek amaçli basit kontrol
                if (request.VerificationCode != "123456")
                    return Result<Guid>.Failure(Error.Validation("Gecersiz dogrulama kodu."));

                // 4. Domain kurallari ile dogrula
                user.VerifyEmail();

                // 5. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(user.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Email dogrulanirken bir hata olustu.", ex.Message));
            }
        }
    }
}