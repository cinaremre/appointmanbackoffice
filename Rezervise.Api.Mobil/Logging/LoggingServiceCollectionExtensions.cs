using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Rezervise.Api.Logging
{
    /// <summary>
    /// Registration helpers for the Graylog GELF sink.
    /// </summary>
    public static class LoggingServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Graylog GELF provider alongside the existing logging providers.
        /// Registration is fully guarded: any failure is reported once on standard error and
        /// the application keeps running with console logging only.
        /// </summary>
        /// <param name="builder">Logging builder supplied by the host.</param>
        /// <param name="configuration">Application configuration root.</param>
        /// <returns>The same logging builder so calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
        public static ILoggingBuilder AddGraylogLogging(this ILoggingBuilder builder, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configuration);

            try
            {
                var options = GelfOptions.FromConfiguration(configuration);

                if (!options.Enabled)
                    return builder;

                builder.AddProvider(new GelfLoggerProvider(options));
            }
            catch (Exception ex)
            {
                // A broken logging configuration must never prevent the API from starting.
                GelfLoggingDiagnostics.WarnOnce($"Graylog logging disabled: {ex.Message}");
            }

            return builder;
        }
    }
}
