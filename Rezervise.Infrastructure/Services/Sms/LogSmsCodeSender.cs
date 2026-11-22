using Rezervise.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Services.Sms
{
    /// <summary>
    /// SMS saglayicisi baglanana kadar kullanilan tasima. Kodu gonder mez,
    /// log'a yazar; boylece OTP akisi uctan uca test edilebilir.
    /// </summary>
    public sealed class LogSmsCodeSender : ISmsCodeSender
    {
        private readonly ILogger<LogSmsCodeSender> _logger;
        private readonly bool _logBody;

        /// <summary>
        /// Tasimayi olusturur.
        /// </summary>
        /// <param name="logger">Log yazici.</param>
        /// <param name="logBody">
        /// true ise kod log'a yazilir. Uretimde false verilmelidir; aksi halde
        /// dogrulama kodu log dosyasindan okunabilir hale gelir.
        /// </param>
        public LogSmsCodeSender(ILogger<LogSmsCodeSender> logger, bool logBody)
        {
            _logger = logger;
            _logBody = logBody;
        }

        /// <inheritdoc />
        public Task SendAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
        {
            // Numara maskelenir; kod yalnizca gelistirme ortaminda gorunur.
            if (_logBody)
                _logger.LogInformation(
                    "SMS OTP (log mode) -> {Phone}: {Code}", MaskPhone(phoneNumber), code);
            else
                _logger.LogInformation("SMS OTP gonderimi atlandi (saglayici yok) -> {Phone}", MaskPhone(phoneNumber));

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
