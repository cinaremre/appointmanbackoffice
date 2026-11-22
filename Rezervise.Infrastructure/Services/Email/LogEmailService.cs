using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Services.Email
{
    /// <summary>
    /// SMTP yapilandirmasi yoksa veya ortam Development ise kullanilan yedek tasima.
    /// E-postayi uygulama log'una yazar; boylece local gelistirmede SMTP sunucusu
    /// kurmadan akis bastan sona test edilebilir.
    /// </summary>
    internal sealed class LogEmailService : IEmailService
    {
        private readonly ILogger<LogEmailService> _logger;
        private readonly bool _includeBody;

        /// <param name="logger">Tanilama kaydedicisi.</param>
        /// <param name="includeBody">
        /// true ise govde (dogrulama bagi dahil) loglanir. Yalnizca Development
        /// ortaminda true verilir; diger ortamlarda token sizmamasi icin false.
        /// </param>
        public LogEmailService(ILogger<LogEmailService> logger, bool includeBody)
        {
            _logger = logger;
            _includeBody = includeBody;
        }

        /// <inheritdoc />
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
                // Govde bilinçli olarak yazilmaz: dogrulama bagi bir kimlik bilgisidir.
                _logger.LogInformation(
                    "E-posta (log modu, govde gizli). To={To} Subject={Subject}",
                    message.To,
                    message.Subject);
            }

            return Task.CompletedTask;
        }
    }
}
