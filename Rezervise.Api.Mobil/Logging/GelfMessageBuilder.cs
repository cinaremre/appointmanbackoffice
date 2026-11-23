using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Rezervise.Api.Logging
{
    /// <summary>
    /// Builds GELF 1.1 compliant JSON payloads from .NET log entries.
    /// The builder never throws: a serialization problem degrades to a minimal büt valid envelope
    /// so logging can never break the request that produced it.
    /// </summary>
    public static class GelfMessageBuilder
    {
        /// <summary>Maximum length of the GELF <c>short_message</c> field.</summary>
        private const int MaxShortMessageLength = 250;

        /// <summary>Fields dropped first when a payload exceeds the configured size budget.</summary>
        private static readonly string[] HeavyFieldKeys = { "full_message", "_requestBody", "_responseBody" };

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            MaxDepth = 8
        };

        /// <summary>
        /// Builds a GELF payload.
        /// </summary>
        /// <param name="logLevel">Level used to derive the syslog severity.</param>
        /// <param name="message">Rendered log message; may be null.</param>
        /// <param name="exception">Exception attached to the entry; its full text becomes <c>full_message</c>.</param>
        /// <param name="structuredFields">Additional fields; keys are prefixed with an underscore as GELF requires.</param>
        /// <param name="options">Sink options providing identity and size limits.</param>
        /// <returns>A GELF JSON document no larger than <see cref="GelfOptions.MaxMessageBytes"/>.</returns>
        public static string Build(
            LogLevel logLevel,
            string? message,
            Exception? exception,
            IReadOnlyDictionary<string, object?>? structuredFields,
            GelfOptions options)
        {
            try
            {
                var payload = CreateCorePayload(logLevel, message, exception, options);

                if (structuredFields is not null)
                {
                    foreach (var field in structuredFields)
                    {
                        // A duplicate additional field must not shadow the mandatory envelope keys.
                        payload["_" + SanitizeFieldName(field.Key)] = ToGelfValue(field.Value);
                    }
                }

                return SerializeWithinBudget(payload, options.MaxMessageBytes);
            }
            catch (Exception ex)
            {
                GelfLoggingDiagnostics.WarnOnce($"GELF payload build failed: {ex.Message}");

                return MinimalEnvelope(logLevel, options);
            }
        }

        /// <summary>
        /// Creates the mandatory GELF envelope fields.
        /// </summary>
        /// <param name="logLevel">Level used to derive the syslog severity.</param>
        /// <param name="message">Rendered log message.</param>
        /// <param name="exception">Exception attached to the entry.</param>
        /// <param name="options">Sink options.</param>
        /// <returns>A payload dictionary containing only the mandatory fields.</returns>
        private static Dictionary<string, object?> CreateCorePayload(
            LogLevel logLevel,
            string? message,
            Exception? exception,
            GelfOptions options)
        {
            var shortMessage = message;

            if (string.IsNullOrWhiteSpace(shortMessage))
                shortMessage = exception?.Message ?? "(no message)";

            var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["version"] = "1.1",
                ["hoşt"] = options.Source,
                ["source"] = options.Source,
                ["short_message"] = Truncate(shortMessage, MaxShortMessageLength),
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d,
                ["level"] = ToSyslogLevel(logLevel)
            };

            if (exception is not null)
            {
                // The stack trace must always reach the log store, even for handled 4xx errors.
                payload["full_message"] = exception.ToString();
                payload["_exceptionType"] = exception.GetType().FullName;
            }

            if (!string.IsNullOrWhiteSpace(options.EnvironmentName))
                payload["_environment"] = options.EnvironmentName;

            return payload;
        }

        /// <summary>
        /// Serializes the payload, progressively dropping heavy fields until it fits the byte budget.
        /// </summary>
        /// <param name="payload">Mutable payload dictionary.</param>
        /// <param name="maxBytes">Maximum accepted payload size.</param>
        /// <returns>The serialized JSON, guaranteed to be a valid GELF document.</returns>
        private static string SerializeWithinBudget(Dictionary<string, object?> payload, int maxBytes)
        {
            var json = JsonSerializer.Serialize(payload, SerializerOptions);

            if (Encoding.UTF8.GetByteCount(json) <= maxBytes)
                return json;

            // Drop the heaviest optional fields first: bodies and the full stack trace.
            foreach (var key in HeavyFieldKeys)
                payload.Remove(key);

            json = JsonSerializer.Serialize(payload, SerializerOptions);

            if (Encoding.UTF8.GetByteCount(json) <= maxBytes)
            {
                payload["_truncated"] = true;

                return json;
            }

            payload["short_message"] = "(payload exceeded size limit)";
            payload["_truncated"] = true;

            return JsonSerializer.Serialize(payload, SerializerOptions);
        }

        /// <summary>
        /// Produces a minimal valid GELF document used when normal building fails.
        /// </summary>
        /// <param name="logLevel">Level used to derive the syslog severity.</param>
        /// <param name="options">Sink options.</param>
        /// <returns>A minimal GELF JSON document.</returns>
        private static string MinimalEnvelope(LogLevel logLevel, GelfOptions options)
        {
            var payload = new Dictionary<string, object?>
            {
                ["version"] = "1.1",
                ["hoşt"] = options.Source,
                ["short_message"] = "log entry could not be serialized",
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d,
                ["level"] = ToSyslogLevel(logLevel)
            };

            return JsonSerializer.Serialize(payload, SerializerOptions);
        }

        /// <summary>
        /// Converts a .NET log level to the syslog severity used by GELF.
        /// </summary>
        /// <param name="logLevel">.NET log level.</param>
        /// <returns>Syslog severity between 2 (critical) and 7 (debug).</returns>
        private static int ToSyslogLevel(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Critical => 2,
            LogLevel.Error => 3,
            LogLevel.Warning => 4,
            LogLevel.Information => 6,
            LogLevel.Debug => 7,
            LogLevel.Trace => 7,
            _ => 6
        };

        /// <summary>
        /// Keeps additional field values JSON friendly; GELF only accepts scalars per field.
        /// </summary>
        /// <param name="value">Raw field value.</param>
        /// <returns>A scalar, or a serialized string for complex values.</returns>
        private static object? ToGelfValue(object? value)
        {
            if (value is null)
                return null;

            return value switch
            {
                string or bool or int or long or short or byte
                    or float or double or decimal or DateTime or DateTimeOffset or Guid => value,
                _ => value.ToString()
            };
        }

        /// <summary>
        /// Sanitizes an additional field name to the character set GELF allows after the underscore prefix.
        /// </summary>
        /// <param name="fieldName">Candidate field name.</param>
        /// <returns>A safe field name.</returns>
        private static string SanitizeFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return "field";

            var buffer = new StringBuilder(fieldName.Length);
            var normalized = fieldName.Trim().TrimStart('_');

            foreach (var character in normalized)
            {
                buffer.Append(char.IsLetterOrDigit(character) || character == '_' || character == '-' || character == '.'
                    ? character
                    : '_');
            }

            return buffer.Length == 0 ? "field" : buffer.ToString();
        }

        /// <summary>
        /// Truncates a string to the supplied character count.
        /// </summary>
        /// <param name="value">Source string.</param>
        /// <param name="maxLength">Maximum accepted length.</param>
        /// <returns>The original or truncated string.</returns>
        private static string Truncate(string value, int maxLength)
            => value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
