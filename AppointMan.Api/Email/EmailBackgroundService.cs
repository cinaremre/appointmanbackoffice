using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppointMan.Api.Email
{
    /// <summary>
    /// E-posta kuyrugunu arka planda bosaltir. Istek thread'ini SMTP beklemesinden
    /// kurtarir. Tek bir mesajin hatasi donguyu durdurmaz.
    /// </summary>
    public sealed class EmailBackgroundService : BackgroundService
    {
        private const int MaxDeliveryAttempts = 3;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

        private readonly IEmailQueueReader _queue;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailBackgroundService> _logger;

        /// <param name="queue">Bosaltilacak kuyruk.</param>
        /// <param name="emailService">Secilen tasima (SMTP veya log).</param>
        /// <param name="logger">Tanilama kaydedicisi.</param>
        public EmailBackgroundService(
            IEmailQueueReader queue,
            IEmailService emailService,
            ILogger<EmailBackgroundService> logger)
        {
            _queue = queue;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Kuyruktan gelen mesajlari sirayla gonderir. Her gonderim yeniden deneme
        /// ve dead-letter kaydiyla sonuclanir; tek mesajin hatasi donguyu durdurmaz.
        /// </summary>
        /// <param name="stoppingToken">Host kapanis belirteci.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var message in _queue.ReadAllAsync(stoppingToken).ConfigureAwait(false))
                {
                    try
                    {
                        await SendWithRetryAsync(message, stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Uygulama kapaniyor; normal akis.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta arka plan servisi beklenmedik sekilde durdu.");
            }
        }

        /// <summary>
        /// Gecici hatalarda yeniden dener; tum denemeler tukenince mesaji
        /// dead-letter olarak yapilandirilmis loga yazar ve kuyrugu boslamaya devam eder.
        /// </summary>
        private async Task SendWithRetryAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            for (var attempt = 1; attempt <= MaxDeliveryAttempts; attempt++)
            {
                try
                {
                    await _emailService.SendAsync(message, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (attempt == MaxDeliveryAttempts)
                    {
                        _logger.LogCritical(
                            ex,
                            "EmailDeadLetter To={To} Subject={Subject} Attempts={Attempts}",
                            message.To,
                            message.Subject,
                            attempt);
                        return;
                    }

                    _logger.LogWarning(
                        ex,
                        "E-posta gonderimi basarisiz, yeniden denenecek. Attempt={Attempt} To={To}",
                        attempt,
                        message.To);

                    try
                    {
                        await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                }
            }
        }
    }
}
