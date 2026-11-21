using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.DeviceTokens.Commands.UnregisterDeviceToken
{
    /// <summary>
    /// Cihaz token'ini gonderime kapatir (cikis / uygulama kaldirma).
    /// Kayit silinmez; pasiflestirilir.
    /// </summary>
    public class UnregisterDeviceTokenCommand : IRequest<Result<bool>>
    {
        /// <summary>Kapatilacak FCM token.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Token sahibi kullanici; JWT kimligiyle eslesmelidir.</summary>
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
            // Token yalnizca sahibi tarafindan kapatilabilir (IDOR korumasi).
            if (_currentUser.UserId is null || _currentUser.UserId != request.UserId)
                return Result<bool>.Failure(Error.Forbidden("Cihaz kaydi yalnizca kendi hesabiniz icin kaldirilabilir."));

            var existing = (await _deviceTokens.FindAsync(
                    t => t.UserId == request.UserId && t.Token == request.Token,
                    cancellationToken))
                .FirstOrDefault();

            // Bilinmeyen token: bilgi sizdirmamak icin sessiz basari.
            if (existing is null)
                return Result<bool>.Success(true);

            existing.Deactivate();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}
