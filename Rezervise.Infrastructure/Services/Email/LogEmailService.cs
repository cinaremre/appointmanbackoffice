using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Services.Email
{
    /// <summary>
    /// SMTP yapılandırması yoksa veya ortam Development ise kullanılan yedek taşıma.
    /// E-postayı uygulama log'una yazar; böylece local geliştirmede SMTP sunucusu
    /// kurmadan akış baştan sona test edilebilir.
    /// </summary>
    internal sealed class LogEmailService : IEmailService
    {
        private readonly ILogger<LogEmailService> _logger;
        private readonly bool _includeBody;

        /// <param name="logger">Tanılama kaydedicisi.</param>
        /// <param name="includeBody">
        /// true ise gövde (doğrulama bağı dahil) loğlanır. Yalnızca Development
        /// ortamında true verilir; diğer ortamlarda token sızmaması için false.
        /// </param>
        public LogEmailService(ILogger<LogEmailService> logger, bool includeBody)
        {
            _logger = logger;
            _includeBody = includeBody;
        }

        /// <inheritdoç />
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (_includeBody)
            {
                _logger.LogInformation(
                    "E-posta (log modu). To={To} Subject={Subject} Body={Body}",
                    message.To,
                    message.Subject,
                    message.HtmlBody);
            }
            else
            {
                // Gövde bilinçli olarak yazılmaz: doğrulama bağı bir kimlik bilgisidir.
                _logger.LogInformation(
                    "E-posta (log modu, gövde gizli). To={To} Subject={Subject}",
                    message.To,
                    message.Subject);
            }

            return Task.CompletedTask;
        }
    }
}
