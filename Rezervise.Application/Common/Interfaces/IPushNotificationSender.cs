using Rezervise.Application.Common.Models;

namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Tek bir push bildiriminin gönderim sonucu.
    /// </summary>
    public record PushSendResult
    {
        /// <summary>Gönderim FCM tarafından kabul edildi mi?</summary>
        public bool Success { get; init; }

        /// <summary>FCM token'i geçersiz/ilişkisi kaldırılmış bildirir (UNREGISTERED).</summary>
        public bool InvalidToken { get; init; }

        /// <summary>Başarısızlıkta sağlayıcı mesajı (log için).</summary>
        public string? Error { get; init; }

        public static PushSendResult Ok => new() { Success = true };
        public static PushSendResult Unregistered => new() { Success = false, InvalidToken = true };
        public static PushSendResult Failed(string error) =>
            new() { Success = false, Error = error };
    }

    /// <summary>
    /// Push bildirimi taşıması soyutlaması (SMS/e-posta deseni ile aynı):
    /// somut sağlayıcı FCM'dir; yapılandırma yoksa log taşıması kullanılır.
    /// </summary>
    public interface IPushNotificationSender
    {
        /// <summary>Tek cihaza bildirim gönderir; taşıma hatası istisna DEGIL, sonuç döner.</summary>
        /// <param name="message">Gönderilecek bildirim.</param>
        /// <param name="cancellationToken">İptal token'i.</param>
        /// <returns>Gönderim sonucu.</returns>
        Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Üretim tarafının yazdığı push kuyruğu. İstek thread'i asla beklemez.
    /// </summary>
    public interface IPushQueue
    {
        /// <summary>Bildirimi kuyruğa alır; kuyruk doluysa reddeder.</summary>
        /// <param name="message">Kuyruğa alınacak bildirim.</param>
        /// <returns>Kabul edilirse true.</returns>
        bool TryEnqueue(PushMessage message);
    }

    /// <summary>Arka plan servislerinin okuduğu push kuyruğu.</summary>
    public interface IPushQueueReader
    {
        /// <summary>Kuyruğu sonlandırılıncaya kadar sırayla okur.</summary>
        /// <param name="cancellationToken">İptal token'i.</param>
        /// <returns>Arka arkaya bildirimler.</returns>
        IAsyncEnumerable<PushMessage> ReadAllAsync(CancellationToken cancellationToken = default);
    }
}
