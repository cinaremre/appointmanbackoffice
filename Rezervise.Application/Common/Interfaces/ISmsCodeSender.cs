namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Telefon doğrulama kodunu SMS ile gönderir. Sağlayıcı bağlanana kadar
    /// log tabanlı implementasyon kullanılır; is akışı sağlayıcıdan bağımsızdır.
    /// </summary>
    public interface ISmsCodeSender
    {
        /// <summary>
        /// Kodu belirtilen numaraya gönderir.
        /// </summary>
        /// <param name="phoneNumber">Alıcı numarası (E.164 / +90... formatı).</param>
        /// <param name="code">Gönderilecek doğrulama kodu.</param>
        /// <param name="cancellationToken">İptal token'i.</param>
        /// <returns>Gönderim tamamlandığında tamamlanan görev.</returns>
        Task SendAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
    }
}
