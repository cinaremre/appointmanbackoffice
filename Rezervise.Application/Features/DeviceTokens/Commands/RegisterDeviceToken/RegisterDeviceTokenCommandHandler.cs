using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.DeviceTokens.Commands.RegisterDeviceToken
{
    /// <summary>
    /// Mobil cihazın FCM token'ini kaydeder. Kimlik JWT'den gelir: kullanıcı
    /// yalnızca KENDI cihazını kaydedebilir. Aynı token tekrar kaydedilirse
    /// görünürlük tazelenir (idempotent upsert).
    /// </summary>
    public class RegisterDeviceTokenCommand : IRequest<Result<Guid>>
    {
        /// <summary>FCM cihaz token'i (40-512 karakter).</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Cihaz platformu (0=Android, 1=iOS).</summary>
        public EnumDevicePlatform Platform { get; set; }

        /// <summary>Atanacak kullanıcı ID'si; JWT kimliğiyle eşleşmelidir.</summary>
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
                // Cihaz kaydı yalnızca kendisi için yapılır (IDOR koruması).
                if (_currentUser.UserId is null || _currentUser.UserId != request.UserId)
                    return Result<Guid>.Failure(Error.Forbidden("Cihaz kaydı yalnızca kendi hesabınız için yapılabilir."));

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
                // Es zamanlı iki kayıt: ikincisi unique index'e takılır; kayıt zaten vardır.
                return Result<Guid>.Failure(Error.Conflict("Cihaz zaten kayıtlı."));
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception)
            {
                return Result<Guid>.Failure(Error.Internal("Cihaz token kaydı sırasında bir hata oluştu."));
            }
        }
    }
}
