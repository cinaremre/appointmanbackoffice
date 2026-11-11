using System.Security.Claims;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Common.Security;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Users.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;

        public RefreshTokenCommandHandler(
            IRepository<User> userRepository,
            ITokenService tokenService,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Eski access token'dan kimligi al (imza, issuer ve audience hala dogrulanir).
                var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
                if (principal is null)
                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Auth.InvalidToken"));

                // 2. Kullanici kimligini coz; bozuk claim 500 degil 401 uretir.
                var userIdClaim = principal.FindFirst("userId")?.Value
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdClaim, out var userId))
                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Auth.InvalidToken"));

                // 3. Kullaniciyi bul
                var users = await _userRepository.FindAsync(
                    u => u.Id == userId && !u.IsDeleted,
                    cancellationToken);

                var user = users.FirstOrDefault();

                if (user is null)
                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Auth.InvalidToken"));

                // 4. Reuse detection: daha once rotate edilmis bir token tekrar sunulursa
                //    token hirsizligi varsayilir ve tum oturumlar iptal edilir.
                if (RefreshTokenHasher.Matches(request.RefreshToken, user.PreviousRefreshTokenHash))
                {
                    user.RevokeRefreshToken(DateTime.UtcNow);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Auth.RefreshTokenReused"));
                }

                // 5. Aktif token dogrulugu: iptal edilmemis, suresi gecmemis ve hash eslesmeli.
                var isActiveToken = user.RefreshTokenRevokedAtUtc is null
                    && user.RefreshTokenExpiresAtUtc.HasValue
                    && user.RefreshTokenExpiresAtUtc.Value > DateTime.UtcNow
                    && RefreshTokenHasher.Matches(request.RefreshToken, user.RefreshTokenHash);

                if (!isActiveToken)
                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Auth.InvalidRefreshToken"));

                // 6. Rotasyon: yeni cift uretilir, eski hash onceki slota tasinir.
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

                return Result<RefreshTokenResponse>.Success(response);
            }
            catch (Exception)
            {
                return Result<RefreshTokenResponse>.Failure(Error.Internal("Auth.RefreshTokenFailed"));
            }
        }
    }
}
