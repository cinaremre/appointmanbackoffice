using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Services.Push
{
    /// <summary>
    /// FCM yapilandirilmamisken kullanilan yedek tasima (SMS/e-posta log modu
    /// deseni ile ayni). Bildirim uygulamasyon log'una yazilir; akis bastan
    /// sona test edilebilir, hicbir dis cagi yapilmaz.
    /// </summary>
    internal sealed class LogPushNotificationSender : IPushNotificationSender
    {
        private readonly ILogger<LogPushNotificationSender> _logger;

        public LogPushNotificationSender(ILogger<LogPushNotificationSender> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            _logger.LogInformation(
                "Push bildirimi (log modu). UserId={UserId} Title={Title} Body={Body} Token={TokenPrefix}...",
                message.UserId,
                message.Title,
                message.Body,
                message.Token.Length > 12 ? message.Token[..12] : message.Token);

            return Task.FromResult(PushSendResult.Ok);
        }
    }
}
