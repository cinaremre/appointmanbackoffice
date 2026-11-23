using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.DeviceTokens.Commands.UnregisterDeviceToken
{
    /// <summary>
    /// Cihaz token'ini gönderime kapatır (çıkış / uygulama kaldırma).
    /// Kayıt silinmez; pasifleştirilir.
    /// </summary>
    public class UnregisterDeviceTokenCommand : IRequest<Result<bool>>
    {
        /// <summary>Kapatılacak FCM token.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Token sahibi kullanıcı; JWT kimliğiyle eşleşmelidir.</summary>
        public Guid UserId { get; set; }
    }

    public class UnregisterDeviceTokenCommandHandler : IRequestHandler<UnregisterDeviceTokenCommand, Result<bool>>
    {
        private readonly IRepository<DeviceToken> _deviceTokens;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public UnregisterDeviceTokenCommandHandler(
            IRepository<DeviceToken> deviceTokens,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _deviceTokens = deviceTokens;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<bool>> Handle(UnregisterDeviceTokenCommand request, CancellationToken cancellationToken)
        {
            // Token yalnızca sahibi tarafından kapatılabilir (IDOR koruması).
            if (_currentUser.UserId is null || _currentUser.UserId != request.UserId)
                return Result<bool>.Failure(Error.Forbidden("Cihaz kaydı yalnızca kendi hesabınız için kaldırılabilir."));

            var existing = (await _deviceTokens.FindAsync(
                    t => t.UserId == request.UserId && t.Token == request.Token,
                    cancellationToken))
                .FirstOrDefault();

            // Bilinmeyen token: bilgi sızdırmamak için sessiz başarı.
            if (existing is null)
                return Result<bool>.Success(true);

            existing.Deactivate();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}
