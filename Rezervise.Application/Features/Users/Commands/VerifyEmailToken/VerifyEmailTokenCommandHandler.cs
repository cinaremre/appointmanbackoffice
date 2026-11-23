using Rezervise.Application.Common.Results;
using Rezervise.Application.Common.Security;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.VerifyEmailToken
{
    /// <summary>
    /// Gelen token'i hash'leyip kayıtlı hash ile eşleştirir, süresini doğrular
    /// ve adresi doğrulanmış olarak işaretler. Token tek kullanımlıktır.
    /// </summary>
    public class VerifyEmailTokenCommandHandler
        : IRequestHandler<VerifyEmailTokenCommand, Result<bool>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        /// <param name="userRepository">Kullanıcı deposu.</param>
        /// <param name="unitOfWork">Kalıcılık birimi.</param>
        public VerifyEmailTokenCommandHandler(IRepository<User> userRepository, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        /// <param name="request">Ham token'i taşıyan komut.</param>
        /// <param name="cancellationToken">İptal belirteci.</param>
        /// <returns>Başarıda <c>true</c>.</returns>
        public async Task<Result<bool>> Handle(
            VerifyEmailTokenCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Boş token ile sorgu yapılmaz.
                if (string.IsNullOrWhiteSpace(request.Token))
                    return Result<bool>.Failure(Error.BusinessRule("Auth.InvalidVerificationToken"));

                // 2. DB'de yalnızca hash saklanır; ham token hash'lenerek eşleştirilir.
                var tokenHash = RefreshTokenHasher.Hash(request.Token.Trim());

                var users = await _userRepository.FindAsync(
                    u => u.EmailVerificationTokenHash == tokenHash && !u.IsDeleted,
                    cancellationToken);

                var user = users.FirstOrDefault();

                if (user is null)
                    return Result<bool>.Failure(Error.BusinessRule("Auth.InvalidVerificationToken"));

                // 3. Süresi geçmiş token reddedilir; yeni bağ istenmelidir.
                if (!user.EmailVerificationTokenExpiresAt.HasValue ||
                    user.EmailVerificationTokenExpiresAt.Value <= DateTime.UtcNow)
                {
                    return Result<bool>.Failure(Error.BusinessRule("Auth.VerificationTokenExpired"));
                }

                // 4. Doğrula: token alanları temizlenir (tek kullanımlık).
                user.VerifyEmail();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (DomainException ex)
            {
                return Result<bool>.Failure(Error.BusinessRule(ex.Key));
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(
                    Error.Internal("E-posta doğrulanırken bir hata oluştu.", ex.Message));
            }
        }
    }
}
