using System.Runtime.CompilerServices;
using System.Threading.Channels;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace AppointMan.Infrastructure.Services.Email
{
    /// <summary>
    /// Sinirli kapasiteli bellek ici e-posta kuyrugu. Istek thread'i asla beklemez:
    /// kuyruk doluysa mesaj reddedilir ve uyari loglanir.
    /// </summary>
    /// <remarks>
    /// Graylog sink'inden farkli olarak <see cref="BoundedChannelFullMode.Wait"/> kullanilir;
    /// cunku <c>DropWrite</c> yazma basarili gorunurken mesaji sessizce atar. E-posta
    /// gozden kacirilmamasi gereken bir islem oldugu icin tasma acikca bildirilir.
    /// </remarks>
    internal sealed class EmailQueue : IEmailQueue, IEmailQueueReader
    {
        private readonly Channel<EmailMessage> _channel;
        private readonly ILogger<EmailQueue> _logger;
        private readonly int _capacity;

        /// <param name="options">Kuyruk kapasitesi kaynagi.</param>
        /// <param name="logger">Tanilama kaydedicisi.</param>
        public EmailQueue(EmailOptions options, ILogger<EmailQueue> logger)
        {
            _logger = logger;
            _capacity = options.QueueCapacity > 0 ? options.QueueCapacity : 500;

            _channel = Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(_capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
        }

        /// <inheritdoc />
        public bool TryEnqueue(EmailMessage message)
        {
            if (message is null)
                return false;

            // Wait modunda TryWrite dolu kuyrukta bloklamaz, dogrudan false doner.
            var accepted = _channel.Writer.TryWrite(message);

            if (!accepted)
            {
                _logger.LogWarning(
                    "E-posta kuyrugu dolu (kapasite {Capacity}); mesaj kuyruga alinamadi. To={To}",
                    _capacity,
                    message.To);
            }

            return accepted;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<EmailMessage> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return message;
            }
        }
    }
}
