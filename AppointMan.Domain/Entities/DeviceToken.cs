using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Bir kullanicinin mobil cihazi icin FCM push token kaydi.
    /// Token asla hassas degil gibi ele alinmaz: yalnizca sahibine bildirim
    /// gondermekte kullanilir. Ayni kullanici + token cifti tek kayittir;
    /// cihaz uygulama kaldirdiginda token pasiflestirilir.
    /// </summary>
    public class DeviceToken : BaseEntity
    {
        public Guid UserId { get; private set; }

        /// <summary>FCM tarafindan uretilen cihaz token'i.</summary>
        public string Token { get; private set; }

        public EnumDevicePlatform Platform { get; private set; }

        /// <summary>Gecersizlesmis (UNREGISTERED donen) token gonderime acik degildir.</summary>
        public bool IsActive { get; private set; }

        public DateTime LastSeenAtUtc { get; private set; }

        private DeviceToken() { } // EF Core

        public DeviceToken(Guid userId, string token, EnumDevicePlatform platform, DateTime nowUtc)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Kullanici ID bos olamaz.", nameof(userId));

            if (string.IsNullOrWhiteSpace(token))
                throw new DomainException("DeviceToken.TokenRequired");

            // FCM token'lari uzun (140-240 karakter) ve degisken uzunluktadir.
            if (token.Length < 40 || token.Length > 512)
                throw new DomainException("DeviceToken.TokenLength");

            UserId = userId;
            Token = token.Trim();
            Platform = platform;
            IsActive = true;
            LastSeenAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        }

        /// <summary>Ayni cihazin tekrar kaydi: gorunurlugu tazeler.</summary>
        public void Touch(DateTime nowUtc)
        {
            LastSeenAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// FCM'den UNREGISTERED donen token'i gonderime kapatir.
        /// Silmek yerine pasiflestirilir; mukerrer kayitlar korunur.
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
