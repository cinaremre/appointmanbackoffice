using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace AppointMan.Application.Features.Users.Commands.VerifyEmail
{
    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<Guid>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public VerifyEmailCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
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

                // 3. Dogrulama kodu kontrolu: kod konfigurasyondan gelir ve tanimli degilse
                //    dogrulama tamamen kapalidir (fail closed). Sabit kod production'a cikmaz.
                var configuredCode = _configuration["EmailVerification:DevelopmentCode"];

                if (string.IsNullOrWhiteSpace(configuredCode))
                    return Result<Guid>.Failure(Error.Validation("EmailVerification.NotConfigured"));

                if (!string.Equals(request.VerificationCode, configuredCode, StringComparison.Ordinal))
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