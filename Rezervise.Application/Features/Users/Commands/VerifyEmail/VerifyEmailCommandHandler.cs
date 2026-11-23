using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Rezervise.Application.Features.Users.Commands.VerifyEmail
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
                // 1. Kullanıcı kontrolü. Silinmiş hesap doğrulanamaz.
                var users = await _userRepository.FindAsync(
                    u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

                var user = users.FirstOrDefault();
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanıcı", request.UserId.ToString()));

                // 2. Zaten doğrulanmış mi?
                if (user.IsEmailVerified)
                    return Result<Guid>.Failure(Error.Conflict("Email zaten doğrulanmış."));

                // 3. Doğrulama kodu kontrolü: kod konfigürasyondan gelir ve tanımlı değilse
                //    doğrulama tamamen kapalıdır (fail closed). Sabit kod production'a çıkmaz.
                var configuredCode = _configuration["EmailVerification:DevelopmentCode"];

                if (string.IsNullOrWhiteSpace(configuredCode))
                    return Result<Guid>.Failure(Error.Validation("EmailVerification.NotConfigured"));

                if (!string.Equals(request.VerificationCode, configuredCode, StringComparison.Ordinal))
                    return Result<Guid>.Failure(Error.Validation("Geçersiz doğrulama kodu."));

                // 4. Domain kuralları ile doğrula
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
                return Result<Guid>.Failure(Error.Internal("Email doğrulanırken bir hata oluştu.", ex.Message));
            }
        }
    }
}
