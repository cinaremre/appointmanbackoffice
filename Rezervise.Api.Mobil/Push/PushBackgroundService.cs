using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Rezervise.Api.Push
{
    /// <summary>
    /// Push kuyruğunu arka planda boşaltır (EmailBackgroundService deseni):
    /// 3 deneme, dead-letter log kaydı, UNREGISTERED token'lar pasifleştirilir.
    /// Tek bir mesajın hatası döngüyü durdurmaz.
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
                // Uygulama kapanıyor; normal akış.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Push arka plan servisi beklenmedik şekilde durdu.");
            }
        }

        /// <summary>
        /// Gönderimi yürütür; sonuç UNREGISTERED ise token kaydını pasifleştirir,
        /// geçici hatalarda 3 deneme yapar, sonra dead-letter loglar.
        /// </summary>
        private async Task SendWithSideEffectsAsync(PushMessage message, CancellationToken cancellationToken)
        {
            PushSendResult? finalResult = null;

            for (var attempt = 1; attempt <= MaxDeliveryAttempts; attempt++)
            {
                var result = await _sender.SendAsync(message, cancellationToken).ConfigureAwait(false);
                finalResult = result;

                // Başarı ya da kalıcı token hatası: yeniden deneme anlamsız.
                if (result.Success || result.InvalidToken)
                    break;

                if (attempt < MaxDeliveryAttempts)
                {
                    _logger.LogWarning(
                        "Push gönderimi başarısız, yeniden denenecek. Attempt={Attempt} UserId={UserId}",
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
        /// FCM'nin geçersiz bildirdiği token'i veritabanında pasifleştirir;
        /// sonraki bildirimler bu cihaza hiç denenmez.
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
                    "Geçersiz push token pasifleştirildi. UserId={UserId}",
                    message.UserId);
            }
            catch (Exception ex)
            {
                // Temizlik hatası gönderim akışını bozmamalı.
                _logger.LogWarning(ex, "Geçersiz push token pasifleştirilemedi. UserId={UserId}", message.UserId);
            }
        }
    }
}
