using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Services.Push
{
    /// <summary>
    /// Sınırlı kapasiteli bellek içi push kuyruğu (EmailQueue ile aynı desen).
    /// İstek thread'i asla beklemez; kuyruk doluysa mesaj reddedilir ve loğlanır.
    /// </summary>
    internal sealed class PushQueue : IPushQueue, IPushQueueReader
    {
        private readonly Channel<PushMessage> _channel;
        private readonly ILogger<PushQueue> _logger;
        private readonly int _capacity;

        public PushQueue(PushSettings settings, ILogger<PushQueue> logger)
        {
            _logger = logger;
            _capacity = settings.QueueCapacity > 0 ? settings.QueueCapacity : 500;

            _channel = Channel.CreateBounded<PushMessage>(new BoundedChannelOptions(_capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
        }

        /// <inheritdoç />
        public bool TryEnqueue(PushMessage message)
        {
            if (message is null)
                return false;

            // Wait modunda TryWrite dolu kuyrukta bloklamaz, false döner.
            var accepted = _channel.Writer.TryWrite(message);

            if (!accepted)
            {
                _logger.LogWarning(
                    "Push kuyruğu dolu (kapasite {Capacity}); bildirim kuyruğa alınamadı. To={UserId}",
                    _capacity,
                    message.UserId);
            }

            return accepted;
        }

        /// <inheritdoç />
        public async IAsyncEnumerable<PushMessage> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken)
                               .ConfigureAwait(false))
            {
                yield return message;
            }
        }
    }
}
