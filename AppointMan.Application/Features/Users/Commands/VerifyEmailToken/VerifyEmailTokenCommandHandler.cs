using AppointMan.Application.Common.Results;
using AppointMan.Application.Common.Security;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Users.Commands.VerifyEmailToken
{
    /// <summary>
    /// Gelen token'i hash'leyip kayitli hash ile eslestirir, suresini dogrular
    /// ve adresi dogrulanmis olarak isaretler. Token tek kullanimliktir.
    /// </summary>
    public class VerifyEmailTokenCommandHandler
        : IRequestHandler<VerifyEmailTokenCommand, Result<bool>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        /// <param name="userRepository">Kullanici deposu.</param>
        /// <param name="unitOfWork">Kalicilik birimi.</param>
        public VerifyEmailTokenCommandHandler(IRepository<User> userRepository, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        /// <param name="request">Ham token'i tasiyan komut.</param>
        /// <param name="cancellationToken">Iptal belirteci.</param>
        /// <returns>Basarida <c>true</c>.</returns>
        public async Task<Result<bool>> Handle(
            VerifyEmailTokenCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Bos token ile sorgu yapilmaz.
                if (string.IsNullOrWhiteSpace(request.Token))
                    return Result<bool>.Failure(Error.BusinessRule("Auth.InvalidVerificationToken"));

                // 2. DB'de yalnizca hash saklanir; ham token hash'lenerek eslestirilir.
                var tokenHash = RefreshTokenHasher.Hash(request.Token.Trim());

                var users = await _userRepository.FindAsync(
                    u => u.EmailVerificationTokenHash == tokenHash && !u.IsDeleted,
                    cancellationToken);

                var user = users.FirstOrDefault();

                if (user is null)
                    return Result<bool>.Failure(Error.BusinessRule("Auth.InvalidVerificationToken"));

                // 3. Suresi gecmis token reddedilir; yeni bag istenmelidir.
                if (!user.EmailVerificationTokenExpiresAt.HasValue ||
                    user.EmailVerificationTokenExpiresAt.Value <= DateTime.UtcNow)
                {
                    return Result<bool>.Failure(Error.BusinessRule("Auth.VerificationTokenExpired"));
                }

                // 4. Dogrula: token alanlari temizlenir (tek kullanimlik).
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
                    Error.Internal("E-posta dogrulanirken bir hata olustu.", ex.Message));
            }
        }
    }
}
