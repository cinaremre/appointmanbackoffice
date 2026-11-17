using AppointMan.Application.Common.Interfaces;
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
        /// Kuyruktan gelen mesajlari sirayla gonderir. Her gonderim kendi
        /// try/catch'i icinde calisir; hata yalnizca loglanir.
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
                        await _emailService.SendAsync(message, stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Tek mesajin hatasi kuyrugu durdurmamali.
                        _logger.LogError(ex, "E-posta kuyrugundan gonderim basarisiz. To={To}", message.To);
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
    }
}
