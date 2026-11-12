using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace AppointMan.Api.Logging
{
    /// <summary>
    /// Sends GELF payloads to Graylog over UDP.
    /// Datagrams are queued in a bounded channel and delivered by a background pump, so the
    /// request thread never blocks on network I/O. When the queue is full messages are dropped
    /// instead of slowing the application down.
    /// </summary>
    internal sealed class GelfLoggerProvider : ILoggerProvider, IGelfSink, IDisposable
    {
        private const int ShutdownGraceMilliseconds = 1000;

        private readonly GelfOptions _options;
        private readonly Channel<byte[]> _channel;
        private readonly UdpClient _udpClient;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly Task _pump;
        private int _disposed;

        /// <summary>
        /// Initialises the provider and starts the background delivery pump.
        /// </summary>
        /// <param name="options">Sink options including the Graylog endpoint.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        internal GelfLoggerProvider(GelfOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(Math.Max(1, _options.QueueCapacity))
            {
                // Dropping the newest entry keeps memory bounded and latency flat under pressure.
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });

            _udpClient = new UdpClient();
            _pump = Task.Run(PumpAsync);
        }

        /// <summary>
        /// Creates a logger that writes GELF documents to this provider's sink.
        /// </summary>
        /// <param name="categoryName">Logger category name.</param>
        /// <returns>A GELF logger instance.</returns>
        public ILogger CreateLogger(string categoryName)
            => new GelfLogger(categoryName, _options, this);

        /// <inheritdoc />
        public bool TryEnqueue(byte[] payload)
        {
            try
            {
                return _channel.Writer.TryWrite(payload);
            }
            catch
            {
                // A full or completed channel is a normal, non-fatal condition.
                return false;
            }
        }

        /// <summary>
        /// Background loop that drains the queue and sends each datagram to Graylog.
        /// </summary>
        /// <returns>A task that completes when the channel is closed or the provider is disposed.</returns>
        private async Task PumpAsync()
        {
            try
            {
                await foreach (var payload in _channel.Reader.ReadAllAsync(_shutdown.Token).ConfigureAwait(false))
                {
                    try
                    {
                        await _udpClient
                            .SendAsync(payload, _options.Host, _options.Port, _shutdown.Token)
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        // UDP is fire and forget: a lost datagram must never break the application.
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
            catch (Exception ex)
            {
                GelfLoggingDiagnostics.WarnOnce($"GELF delivery pump stopped: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                // Complete the writer first so queued entries still get a chance to be flushed.
                _channel.Writer.TryComplete();
                _pump.Wait(ShutdownGraceMilliseconds);
            }
            catch
            {
                // Shutdown must stay silent.
            }
            finally
            {
                try
                {
                    _shutdown.Cancel();
                    _udpClient.Dispose();
                    _shutdown.Dispose();
                }
                catch
                {
                    // Disposing a network primitive is best effort.
                }
            }
        }
    }
}
