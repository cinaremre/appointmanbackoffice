using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Services.Email
{
    /// <summary>
    /// Sınırlı kapasiteli bellek içi e-posta kuyruğu. İstek thread'i asla beklemez:
    /// kuyruk doluysa mesaj reddedilir ve uyarı loğlanır.
    /// </summary>
    /// <remarks>
    /// Graylog sink'inden farklı olarak <see cref="BoundedChannelFullMode.Wait"/> kullanılır;
    /// çünkü <c>DropWrite</c> yazma başarılı görünürken mesajı sessizce atar. E-posta
    /// gözden kaçırılmaması gereken bir işlem öldüğü için taşma açıkça bildirilir.
    /// </remarks>
    internal sealed class EmailQueue : IEmailQueue, IEmailQueueReader
    {
        private readonly Channel<EmailMessage> _channel;
        private readonly ILogger<EmailQueue> _logger;
        private readonly int _capacity;

        /// <param name="options">Kuyruk kapasitesi kaynağı.</param>
        /// <param name="logger">Tanılama kaydedicisi.</param>
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

        /// <inheritdoç />
        public bool TryEnqueue(EmailMessage message)
        {
            if (message is null)
                return false;

            // Wait modunda TryWrite dolu kuyrukta bloklamaz, doğrudan false döner.
            var accepted = _channel.Writer.TryWrite(message);

            if (!accepted)
            {
                _logger.LogWarning(
                    "E-posta kuyruğu dolu (kapasite {Capacity}); mesaj kuyruğa alınamadı. To={To}",
                    _capacity,
                    message.To);
            }

            return accepted;
        }

        /// <inheritdoç />
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
