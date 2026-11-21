using AppointMan.Application.Common.Models;

namespace AppointMan.Application.Common.Interfaces
{
    /// <summary>
    /// Tek bir push bildiriminin gonderim sonucu.
    /// </summary>
    public record PushSendResult
    {
        /// <summary>Gonderim FCM tarafindan kabul edildi mi?</summary>
        public bool Success { get; init; }

        /// <summary>FCM token'i gecersiz/iliskisi kaldirilmis bildirir (UNREGISTERED).</summary>
        public bool InvalidToken { get; init; }

        /// <summary>Basarisizlikta saglayici mesaji (log icin).</summary>
        public string? Error { get; init; }

        public static PushSendResult Ok => new() { Success = true };
        public static PushSendResult Unregistered => new() { Success = false, InvalidToken = true };
        public static PushSendResult Failed(string error) =>
            new() { Success = false, Error = error };
    }

    /// <summary>
    /// Push bildirimi tasimasi soyutlamasi (SMS/e-posta deseni ile ayni):
    /// somut saglayici FCM'dir; yapilandirma yoksa log tasimasi kullanilir.
    /// </summary>
    public interface IPushNotificationSender
    {
        /// <summary>Tek cihaza bildirim gonderir; tasima hatasi istisna DEGIL, sonuc doner.</summary>
        /// <param name="message">Gonderilecek bildirim.</param>
        /// <param name="cancellationToken">Iptal token'i.</param>
        /// <returns>Gonderim sonucu.</returns>
        Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Uretim tarafinin yazdigi push kuyrugu. Istek thread'i asla beklemez.
    /// </summary>
    public interface IPushQueue
    {
        /// <summary>Bildirimi kuyruga alir; kuyruk doluysa reddeder.</summary>
        /// <param name="message">Kuyruga alinacak bildirim.</param>
        /// <returns>Kabul edilirse true.</returns>
        bool TryEnqueue(PushMessage message);
    }

    /// <summary>Arka plan servislerinin okudugu push kuyrugu.</summary>
    public interface IPushQueueReader
    {
        /// <summary>Kuyrugu sonlandirilincaya kadar sirayla okur.</summary>
        /// <param name="cancellationToken">Iptal token'i.</param>
        /// <returns>Arka arkaya bildirimler.</returns>
        IAsyncEnumerable<PushMessage> ReadAllAsync(CancellationToken cancellationToken = default);
    }
}
