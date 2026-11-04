using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
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
                // 1. Eski token'dan principal al
                var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
                if (principal is null)
                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Geçersiz token."));

                // 2. Kullanıcı ID'sini al
                var userId = principal.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Geçersiz token."));

                // 3. Kullanıcıyı bul
                var users = await _userRepository.FindAsync(
                    u => u.Id == Guid.Parse(userId) && !u.IsDeleted,
                    cancellationToken);
                var user = users.FirstOrDefault();

                if (user is null)
                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Kullanıcı bulunamadı."));

                // 4. Refresh token kontrolü
                if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                    return Result<RefreshTokenResponse>.Failure(Error.Unauthorized("Geçersiz refresh token."));

                // 5. Yeni token oluştur
                var newAccessToken = _tokenService.GenerateAccessToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                // 6. Refresh token'ı güncelle
                user.SetRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7));
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new RefreshTokenResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Expiration = DateTime.UtcNow.AddMinutes(60)
                };

                return Result<RefreshTokenResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Result<RefreshTokenResponse>.Failure(Error.Internal("Token yenilenirken bir hata oluştu.", ex.Message));
            }
        }
    }
}