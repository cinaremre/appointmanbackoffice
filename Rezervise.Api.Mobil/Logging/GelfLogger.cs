using System.Text;
using Microsoft.Extensions.Logging;

namespace Rezervise.Api.Logging
{
    /// <summary>
    /// Emits log entries to a <see cref="IGelfSink"/> as GELF documents.
    /// Every operation is wrapped so that a logging failure can never surface to the caller.
    /// </summary>
    internal sealed class GelfLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly GelfOptions _options;
        private readonly IGelfSink _sink;

        /// <summary>
        /// Initialises the logger.
        /// </summary>
        /// <param name="categoryName">Logger category name reported as an additional field.</param>
        /// <param name="options">Sink options.</param>
        /// <param name="sink">Non-blocking hand-off point for serialized payloads.</param>
        internal GelfLogger(string categoryName, GelfOptions options, IGelfSink sink)
        {
            _categoryName = categoryName;
            _options = options;
            _sink = sink;
        }

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            // Scopes are not propagated to GELF: the middleware already adds every
            // correlation and request field explicitly. Keeping this a no-op avoids
            // value boxing on the request hot path.
            return null;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
            => _options.Enabled && logLevel != LogLevel.None;

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            try
            {
                if (!IsEnabled(logLevel) || formatter is null)
                    return;

                var message = formatter(state, exception);
                var fields = ExtractStructuredFields(state, eventId);

                var json = GelfMessageBuilder.Build(logLevel, message, exception, fields, _options);

                _sink.TryEnqueue(Encoding.UTF8.GetBytes(json));
            }
            catch (Exception ex)
            {
                // Logging is best effort: a failure here must never affect the request.
                GelfLoggingDiagnostics.WarnOnce($"GELF log write failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts the log state into GELF additional fields.
        /// ASP.NET Core renders message templates into a key/value list, which maps directly
        /// onto GELF fields such as <c>_correlationId</c> or <c>_userId</c>.
        /// </summary>
        /// <typeparam name="TState">Log state type.</typeparam>
        /// <param name="state">Log state supplied by the caller.</param>
        /// <param name="eventId">Event identifier supplied by the caller.</param>
        /// <returns>Additional fields, or null when the state carries none.</returns>
        private Dictionary<string, object?>? ExtractStructuredFields<TState>(TState state, EventId eventId)
        {
            Dictionary<string, object?>? fields = null;

            try
            {
                fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["logger"] = _categoryName
                };

                if (eventId.Id != 0)
                    fields["eventId"] = eventId.Id;

                if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
                {
                    foreach (var pair in pairs)
                    {
                        // {OriginalFormat} duplicates the rendered message; it adds no value downstream.
                        if (string.IsNullOrWhiteSpace(pair.Key) || pair.Key == "{OriginalFormat}")
                            continue;

                        fields[pair.Key.Trim('{', '}')] = pair.Value;
                    }
                }

                return fields;
            }
            catch
            {
                // Malformed state must not prevent the entry from being logged.
                return fields;
            }
        }
    }
}
