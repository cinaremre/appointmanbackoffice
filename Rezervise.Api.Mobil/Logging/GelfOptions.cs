using Microsoft.Extensions.Configuration;

namespace Rezervise.Api.Logging
{
    /// <summary>
    /// Configuration for the Graylog GELF/UDP logging sink.
    /// Values are read from environment variables first (<c>GRAYLOG_*</c>) and fall back
    /// to the <c>Graylog</c> configuration section.
    /// </summary>
    public sealed class GelfOptions
    {
        /// <summary>Configuration section name used when environment variables are absent.</summary>
        public const string SectionName = "Graylog";

        /// <summary>Default Graylog GELF UDP port.</summary>
        public const int DefaultPort = 12201;

        /// <summary>Default bound of the in-memory queue holding pending datagrams.</summary>
        public const int DefaultQueueCapacity = 5000;

        /// <summary>
        /// Maximum payload size accepted by the sink. GELF over UDP messages must stay
        /// below the practical datagram limit, otherwise routers may drop them.
        /// </summary>
        public const int DefaultMaxMessageBytes = 60000;

        /// <summary>
        /// Field names that must never reach the log store in clear text.
        /// Reused by the masking layer so the list lives in exactly one place.
        /// </summary>
        public static readonly IReadOnlyList<string> DefaultSensitiveFields = new[]
        {
            "password",
            "passwordHash",
            "currentPassword",
            "newPassword",
            "token",
            "accessToken",
            "refreshToken",
            "refreshTokenHash",
            "authorization",
            "secret",
            "apiKey",
            "apiKeyHash",
            "creditCard",
            "cardNumber",
            "cvv",
            "cvc",
            "tckn",
            "tcKimlikNo",
            "iban"
        };

        /// <summary>When false the provider is not registered at all (true no-op).</summary>
        public bool Enabled { get; init; }

        /// <summary>Graylog GELF UDP endpoint host.</summary>
        public string Host { get; init; } = "localhost";

        /// <summary>Graylog GELF UDP endpoint port.</summary>
        public int Port { get; init; } = DefaultPort;

        /// <summary>Value reported as both the GELF <c>host</c> and <c>source</c> field.</summary>
        public string Source { get; init; } = "Rezervise.Api";

        /// <summary>Deployment environment reported as the <c>_environment</c> field.</summary>
        public string EnvironmentName { get; init; } = string.Empty;

        /// <summary>Payloads larger than this many bytes are trimmed before sending.</summary>
        public int MaxMessageBytes { get; init; } = DefaultMaxMessageBytes;

        /// <summary>Bound of the in-memory queue. Full queues drop messages instead of blocking callers.</summary>
        public int QueueCapacity { get; init; } = DefaultQueueCapacity;

        /// <summary>Additional sensitive field names supplied through configuration.</summary>
        public IReadOnlyList<string> MaskedFields { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Default sensitive fields merged with the configured extras, de-duplicated case-insensitively.
        /// </summary>
        public IReadOnlyList<string> EffectiveMaskedFields
        {
            get
            {
                var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 'field' cannot be used as a local name inside a property accessor in C# 14.
                foreach (var fieldName in DefaultSensitiveFields)
                    merged.Add(fieldName);

                foreach (var fieldName in MaskedFields)
                {
                    if (!string.IsNullOrWhiteSpace(fieldName))
                        merged.Add(fieldName.Trim());
                }

                return merged.ToArray();
            }
        }

        /// <summary>
        /// Builds options from configuration, giving environment variables priority over the
        /// <c>Graylog</c> section so container deployments can override file values.
        /// </summary>
        /// <param name="configuration">Application configuration root.</param>
        /// <returns>A fully populated options instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
        public static GelfOptions FromConfiguration(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var section = configuration.GetSection(SectionName);

            return new GelfOptions
            {
                Enabled = ParseBool(configuration["GRAYLOG_ENABLED"])
                          ?? ParseBool(section["Enabled"])
                          ?? false,
                Host = FirstNonEmpty(configuration["GRAYLOG_HOST"], section["Host"]) ?? "localhost",
                Port = ParseInt(configuration["GRAYLOG_PORT"])
                       ?? ParseInt(section["Port"])
                       ?? DefaultPort,
                Source = FirstNonEmpty(configuration["GRAYLOG_SOURCE"], section["Source"]) ?? "Rezervise.Api",
                EnvironmentName = FirstNonEmpty(configuration["GRAYLOG_ENVIRONMENT"], section["Environment"]) ?? string.Empty,
                MaxMessageBytes = ParseInt(section["MaxMessageBytes"]) ?? DefaultMaxMessageBytes,
                QueueCapacity = ParseInt(section["QueueCapacity"]) ?? DefaultQueueCapacity,
                MaskedFields = ParseList(configuration["GRAYLOG_MASKED_FIELDS"], section.GetSection("MaskedFields"))
            };
        }

        /// <summary>
        /// Reads a comma or semicolon separated list from an environment variable and merges it
        /// with the values declared in the configuration section.
        /// </summary>
        /// <param name="fromEnvironment">Raw environment variable value; may be null.</param>
        /// <param name="fromSection">Configuration section holding an array of field names.</param>
        /// <returns>Combined, de-duplicated field list.</returns>
        private static IReadOnlyList<string> ParseList(string? fromEnvironment, IConfigurationSection fromSection)
        {
            var result = new List<string>();

            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                var separators = new[] { ',', ';' };

                foreach (var part in fromEnvironment.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    result.Add(part);
            }

            foreach (var child in fromSection.GetChildren())
            {
                if (!string.IsNullOrWhiteSpace(child.Value))
                    result.Add(child.Value.Trim());
            }

            return result;
        }

        /// <summary>Parses a boolean configuration value, returning null when absent or malformed.</summary>
        /// <param name="value">Raw configuration value.</param>
        /// <returns>The parsed value, or null.</returns>
        private static bool? ParseBool(string? value)
            => bool.TryParse(value, out var parsed) ? parsed : null;

        /// <summary>Parses an integer configuration value, returning null when absent or malformed.</summary>
        /// <param name="value">Raw configuration value.</param>
        /// <returns>The parsed value, or null.</returns>
        private static int? ParseInt(string? value)
            => int.TryParse(value, out var parsed) ? parsed : null;

        /// <summary>Returns the first non-empty candidate.</summary>
        /// <param name="primary">Preferred value.</param>
        /// <param name="fallback">Value used when the primary is empty.</param>
        /// <returns>The first non-empty value, or null.</returns>
        private static string? FirstNonEmpty(string? primary, string? fallback)
            => !string.IsNullOrWhiteSpace(primary)
                ? primary
                : string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }
}
