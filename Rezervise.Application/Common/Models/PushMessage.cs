namespace Rezervise.Application.Common.Models
{
    /// <summary>
    /// Kuyruga alinan tek push bildirimi. Alici cihaz token'idir;
    /// kullanici ID'si yalnizca denetim ve invalid-token temizligi icin tasir.
    /// </summary>
    public class PushMessage
    {
        /// <summary>Hedef FCM cihaz token'i.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Bildirimin sahibi; UNREGISTERED temizliginde kullanilir.</summary>
        public Guid UserId { get; set; }

        /// <summary>Bildirim basligi.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Bildirim govdesi.</summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>Derin baglanti ve tur gibi ek veriler (key/value).</summary>
        public IReadOnlyDictionary<string, string>? Data { get; set; }
    }
}
