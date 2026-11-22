using System.Net;
using System.Net.Mail;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Services.Email
{
    /// <summary>
    /// BCL <see cref="SmtpClient"/> tabanli gonderim. Ek NuGet paketi gerektirmez.
    /// Gecici tasima hatalari firlatilir; yeniden deneme ve dead-letter kaydi
    /// <see cref="Api.Email.EmailBackgroundService"/> tarafindan yapilir.
    /// </summary>
    internal sealed class SmtpEmailService : IEmailService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<SmtpEmailService> _logger;

        /// <param name="options">SMTP ayarlari.</param>
        /// <param name="logger">Tanilama kaydedicisi.</param>
        public SmtpEmailService(EmailOptions options, ILogger<SmtpEmailService> logger)
        {
            _options = options;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseSsl,
                Timeout = _options.SmtpTimeoutMilliseconds,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            // Kimlik bilgisi bos ise anonim gonderim denenir (ic SMTP relay senaryosu).
            if (!string.IsNullOrWhiteSpace(_options.UserName))
                client.Credentials = new NetworkCredential(_options.UserName, _options.Password);

            using var mail = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = message.Subject,
                Body = message.HtmlBody,
                IsBodyHtml = true
            };

            mail.To.Add(message.To);

            if (!string.IsNullOrWhiteSpace(message.TextBody))
                mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                    message.TextBody, null, "text/plain"));

            await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("E-posta gonderildi. To={To}", message.To);
        }
    }
}
