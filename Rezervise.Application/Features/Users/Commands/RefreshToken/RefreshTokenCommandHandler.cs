using System.Security.Claims;
using Rezervise.Application.Common.Diagnostics;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Common.Security;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly RezerviseMetrics _metrics;

        public RefreshTokenCommandHandler(
            IRepository<User> userRepository,
            ITokenService tokenService,
            IUnitOfWork unitOfWork,
            RezerviseMetrics metrics)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
            _metrics = metrics;
        }

        public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Eski access token'dan kimliği al (imza, issuer ve audience hala doğrulanır).
                var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
                if (principal is null)
                {
                    _metrics.RecordRefreshTokenRotation("invalid");
                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Auth.InvalidToken"));
                }

                // 2. Kullanıcı kimliğini çöz; bozuk claim 500 değil 401 üretir.
                var userIdClaim = principal.FindFirst("userId")?.Value
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    _metrics.RecordRefreshTokenRotation("invalid");
                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Auth.InvalidToken"));
                }

                // 3. Kullanıcıyı bul
                var users = await _userRepository.FindAsync(
                    u => u.Id == userId && !u.IsDeleted,
                    cancellationToken);

                var user = users.FirstOrDefault();

                if (user is null)
                {
                    _metrics.RecordRefreshTokenRotation("invalid");
                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Auth.InvalidToken"));
                }

                // 4. Reuse detection: daha önce rotate edilmiş bir token tekrar sunulursa
                //    token hırsızlığı varsayılır ve tüm oturumlar iptal edilir.
                if (RefreshTokenHasher.Matches(request.RefreshToken, user.PreviousRefreshTokenHash))
                {
                    user.RevokeRefreshToken(DateTime.UtcNow);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _metrics.RecordRefreshTokenRotation("reused");

                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Auth.RefreshTokenReused"));
                }

                // 5. Aktif token doğruluğu: iptal edilmemiş, süresi geçmemiş ve hash eşleşmeli.
                var isActiveToken = user.RefreshTokenRevokedAtUtc is null
                    && user.RefreshTokenExpiresAtUtc.HasValue
                    && user.RefreshTokenExpiresAtUtc.Value > DateTime.UtcNow
                    && RefreshTokenHasher.Matches(request.RefreshToken, user.RefreshTokenHash);

                if (!isActiveToken)
                {
                    // Distinguish a stale token from a rejected one so the dashboard can tell them apart.
                    var outcome = user.RefreshTokenExpiresAtUtc.HasValue
                                  && user.RefreshTokenExpiresAtUtc.Value <= DateTime.UtcNow
                        ? "expired"
                        : "invalid";

                    _metrics.RecordRefreshTokenRotation(outcome);

                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Auth.InvalidRefreshToken"));
                }

                // 6. Rotasyon: yeni çift üretilir, eski hash önceki slota taşınır.
                var newAccessToken = _tokenService.GenerateAccessToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                user.IssueRefreshToken(
                    RefreshTokenHasher.Hash(newRefreshToken),
                    _tokenService.GetRefreshTokenExpiryUtc(),
                    DateTime.UtcNow);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new RefreshTokenResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Expiration = _tokenService.GetAccessTokenExpiryUtc()
                };

                _metrics.RecordRefreshTokenRotation("success");

                return Result<RefreshTokenResponse>.Success(response);
            }
            catch (Exception)
            {
                return Result<RefreshTokenResponse>.Failure(Error.Internal("Auth.RefreshTokenFailed"));
            }
        }
    }
}
