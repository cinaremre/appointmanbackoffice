using Rezervise.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Services.Sms
{
    /// <summary>
    /// SMS sağlayıcısı bağlanana kadar kullanılan taşıma. Kodu gönder mez,
    /// log'a yazar; böylece OTP akışı uçtan uça test edilebilir.
    /// </summary>
    public sealed class LogSmsCodeSender : ISmsCodeSender
    {
        private readonly ILogger<LogSmsCodeSender> _logger;
        private readonly bool _logBody;

        /// <summary>
        /// Taşımayı oluşturur.
        /// </summary>
        /// <param name="logger">Log yazıcı.</param>
        /// <param name="logBody">
        /// true ise kod log'a yazılır. Üretimde false verilmelidir; aksi halde
        /// doğrulama kodu log dosyasından okunabilir hale gelir.
        /// </param>
        public LogSmsCodeSender(ILogger<LogSmsCodeSender> logger, bool logBody)
        {
            _logger = logger;
            _logBody = logBody;
        }

        /// <inheritdoç />
        public Task SendAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
        {
            // Numara maskelenir; kod yalnızca geliştirme ortamında görünür.
            if (_logBody)
                _logger.LogInformation(
                    "SMS OTP (log mode) -> {Phone}: {Code}", MaskPhone(phoneNumber), code);
            else
                _logger.LogInformation("SMS OTP gönderimi atlandı (sağlayıcı yok) -> {Phone}", MaskPhone(phoneNumber));

            return Task.CompletedTask;
        }

        private static string MaskPhone(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length <= 4)
                return "***";

            return string.Concat(phoneNumber.AsSpan(0, phoneNumber.Length - 4), "****");
        }
    }
}
