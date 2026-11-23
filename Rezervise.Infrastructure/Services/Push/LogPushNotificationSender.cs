using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Services.Push
{
    /// <summary>
    /// FCM yapılandırılmamışken kullanılan yedek taşıma (SMS/e-posta log modu
    /// deseni ile aynı). Bildirim uygulamasyon log'una yazılır; akış baştan
    /// sona test edilebilir, hiçbir dış çağı yapılmaz.
    /// </summary>
    internal sealed class LogPushNotificationSender : IPushNotificationSender
    {
        private readonly ILogger<LogPushNotificationSender> _logger;

        public LogPushNotificationSender(ILogger<LogPushNotificationSender> logger)
        {
            _logger = logger;
        }

        /// <inheritdoç />
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
