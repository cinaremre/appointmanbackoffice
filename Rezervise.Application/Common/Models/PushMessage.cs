namespace Rezervise.Application.Common.Models
{
    /// <summary>
    /// Kuyruğa alınan tek push bildirimi. Alıcı cihaz token'idir;
    /// kullanıcı ID'si yalnızca denetim ve invalid-token temizliği için taşır.
    /// </summary>
    public class PushMessage
    {
        /// <summary>Hedef FCM cihaz token'i.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Bildirimin sahibi; UNREGISTERED temizliğinde kullanılır.</summary>
        public Guid UserId { get; set; }

        /// <summary>Bildirim başlığı.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Bildirim gövdesi.</summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>Derin bağlantı ve tur gibi ek veriler (key/value).</summary>
        public IReadOnlyDictionary<string, string>? Data { get; set; }
    }
}
