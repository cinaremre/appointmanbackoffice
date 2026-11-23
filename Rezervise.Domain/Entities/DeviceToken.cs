using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Bir kullanıcının mobil cihazı için FCM push token kaydı.
    /// Token asla hassas değil gibi ele alınmaz: yalnızca sahibine bildirim
    /// göndermekte kullanılır. Aynı kullanıcı + token çifti tek kayıttır;
    /// cihaz uygulama kaldırdığında token pasifleştirilir.
    /// </summary>
    public class DeviceToken : BaseEntity
    {
        public Guid UserId { get; private set; }

        /// <summary>FCM tarafından üretilen cihaz token'i.</summary>
        public string Token { get; private set; }

        public EnumDevicePlatform Platform { get; private set; }

        /// <summary>Geçersizleşmiş (UNREGISTERED dönen) token gönderime açık değildir.</summary>
        public bool IsActive { get; private set; }

        public DateTime LastSeenAtUtc { get; private set; }

        private DeviceToken() { } // EF Core

        public DeviceToken(Guid userId, string token, EnumDevicePlatform platform, DateTime nowUtc)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Kullanıcı ID boş olamaz.", nameof(userId));

            if (string.IsNullOrWhiteSpace(token))
                throw new DomainException("DeviceToken.TokenRequired");

            // FCM token'lari uzun (140-240 karakter) ve değişken uzunluktadır.
            if (token.Length < 40 || token.Length > 512)
                throw new DomainException("DeviceToken.TokenLength");

            UserId = userId;
            Token = token.Trim();
            Platform = platform;
            IsActive = true;
            LastSeenAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        }

        /// <summary>Aynı cihazın tekrar kaydı: görünürlüğü tazeler.</summary>
        public void Touch(DateTime nowUtc)
        {
            LastSeenAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// FCM'den UNREGISTERED dönen token'i gönderime kapatır.
        /// Silmek yerine pasifleştirilir; mükerrer kayıtlar korunur.
        /// </summary>
        public void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
