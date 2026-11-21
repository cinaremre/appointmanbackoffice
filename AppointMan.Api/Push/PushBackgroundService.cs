using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Models;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppointMan.Api.Push
{
    /// <summary>
    /// Push kuyrugunu arka planda bosaltir (EmailBackgroundService deseni):
    /// 3 deneme, dead-letter log kaydi, UNREGISTERED token'lar pasiflestirilir.
    /// Tek bir mesajin hatasi donguyu durdurmaz.
    /// </summary>
    public sealed class PushBackgroundService : BackgroundService
    {
        private const int MaxDeliveryAttempts = 3;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

        private readonly IPushQueueReader _queue;
        private readonly IPushNotificationSender _sender;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PushBackgroundService> _logger;

        public PushBackgroundService(
            IPushQueueReader queue,
            IPushNotificationSender sender,
            IServiceScopeFactory scopeFactory,
            ILogger<PushBackgroundService> logger)
        {
            _queue = queue;
            _sender = sender;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var message in _queue.ReadAllAsync(stoppingToken).ConfigureAwait(false))
                {
                    try
                    {
                        await SendWithSideEffectsAsync(message, stoppingToken).ConfigureAwait(false);
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
                _logger.LogError(ex, "Push arka plan servisi beklenmedik sekilde durdu.");
            }
        }

        /// <summary>
        /// Gonderimi yurutur; sonuc UNREGISTERED ise token kaydini pasiflestirir,
        /// gecici hatalarda 3 deneme yapar, sonra dead-letter loglar.
        /// </summary>
        private async Task SendWithSideEffectsAsync(PushMessage message, CancellationToken cancellationToken)
        {
            PushSendResult? finalResult = null;

            for (var attempt = 1; attempt <= MaxDeliveryAttempts; attempt++)
            {
                var result = await _sender.SendAsync(message, cancellationToken).ConfigureAwait(false);
                finalResult = result;

                // Basari ya da kalici token hatasi: yeniden deneme anlamsiz.
                if (result.Success || result.InvalidToken)
                    break;

                if (attempt < MaxDeliveryAttempts)
                {
                    _logger.LogWarning(
                        "Push gonderimi basarisiz, yeniden denenecek. Attempt={Attempt} UserId={UserId}",
                        attempt,
                        message.UserId);

                    try
                    {
                        await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }

            var outcome = finalResult!;

            if (outcome.Success)
                return;

            if (outcome.InvalidToken)
            {
                await DeactivateTokenAsync(message, cancellationToken).ConfigureAwait(false);
                return;
            }

            _logger.LogCritical(
                "PushDeadLetter UserId={UserId} Title={Title} Attempts={Attempts} Error={Error}",
                message.UserId,
                message.Title,
                MaxDeliveryAttempts,
                outcome.Error);
        }

        /// <summary>
        /// FCM'nin gecersiz bildirdigi token'i veritabaninda pasiflestirir;
        /// sonraki bildirimler bu cihaza hic denenmez.
        /// </summary>
        private async Task DeactivateTokenAsync(PushMessage message, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IRepository<DeviceToken>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var entry = (await repository.FindAsync(
                        t => t.UserId == message.UserId && t.Token == message.Token,
                        cancellationToken))
                    .FirstOrDefault();

                if (entry is null || !entry.IsActive)
                    return;

                entry.Deactivate();
                await unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Gecersiz push token pasiflestirildi. UserId={UserId}",
                    message.UserId);
            }
            catch (Exception ex)
            {
                // Temizlik hatasi gonderim akisini bozmamali.
                _logger.LogWarning(ex, "Gecersiz push token pasiflestirilemedi. UserId={UserId}", message.UserId);
            }
        }
    }
}
