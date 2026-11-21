using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.DeviceTokens.Commands.RegisterDeviceToken
{
    /// <summary>
    /// Mobil cihazin FCM token'ini kaydeder. Kimlik JWT'den gelir: kullanici
    /// yalnizca KENDI cihazini kaydedebilir. Ayni token tekrar kaydedilirse
    /// gorunurluk tazelenir (idempotent upsert).
    /// </summary>
    public class RegisterDeviceTokenCommand : IRequest<Result<Guid>>
    {
        /// <summary>FCM cihaz token'i (40-512 karakter).</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Cihaz platformu (0=Android, 1=iOS).</summary>
        public EnumDevicePlatform Platform { get; set; }

        /// <summary>Atanacak kullanici ID'si; JWT kimligiyle eslesmelidir.</summary>
        public Guid UserId { get; set; }
    }

    public class RegisterDeviceTokenCommandHandler : IRequestHandler<RegisterDeviceTokenCommand, Result<Guid>>
    {
        private readonly IRepository<DeviceToken> _deviceTokens;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTime;

        public RegisterDeviceTokenCommandHandler(
            IRepository<DeviceToken> deviceTokens,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTime)
        {
            _deviceTokens = deviceTokens;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Result<Guid>> Handle(RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Cihaz kaydi yalnizca kendisi icin yapilir (IDOR korumasi).
                if (_currentUser.UserId is null || _currentUser.UserId != request.UserId)
                    return Result<Guid>.Failure(Error.Forbidden("Cihaz kaydi yalnizca kendi hesabiniz icin yapilabilir."));

                var now = _dateTime.UtcNow;

                var existing = (await _deviceTokens.FindAsync(
                        t => t.UserId == request.UserId && t.Token == request.Token,
                        cancellationToken))
                    .FirstOrDefault();

                if (existing is not null)
                {
                    existing.Touch(now);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return Result<Guid>.Success(existing.Id);
                }

                var entry = new DeviceToken(request.UserId, request.Token, request.Platform, now);
                await _deviceTokens.AddAsync(entry, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(entry.Id);
            }
            catch (DuplicateKeyException)
            {
                // Es zamanli iki kayit: ikincisi unique index'e takilir; kayit zaten vardir.
                return Result<Guid>.Failure(Error.Conflict("Cihaz zaten kayitli."));
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception)
            {
                return Result<Guid>.Failure(Error.Internal("Cihaz token kaydi sirasinda bir hata olustu."));
            }
        }
    }
}
