using Rezervise.Application.Common.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Rezervise.Api.Metrics
{
    /// <summary>
    /// Registration helpers for the OpenTelemetry metrics pipeline and the Prometheus exporter.
    /// </summary>
    public static class MetricsServiceCollectionExtensions
    {
        /// <summary>
        /// EF Core publishes its own metrics through this meter; no extra instrumentation
        /// package is required for database timings.
        /// </summary>
        private const string EntityFrameworkCoreMeterName = "Microsoft.EntityFrameworkCore";

        /// <summary>
        /// Registers OpenTelemetry metrics with the Prometheus exporter.
        /// Registration is fully guarded: a failure is reported once on standard error and the
        /// application keeps running without metrics instead of failing to start.
        /// </summary>
        /// <param name="services">Target service collection.</param>
        /// <param name="configuration">Application configuration root.</param>
        /// <returns>The same service collection so calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
        public static IServiceCollection AddRezerviseMetrics(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.Configure<MetricsSettings>(configuration.GetSection(MetricsSettings.SectionName));

            try
            {
                var settings = configuration.GetSection(MetricsSettings.SectionName).Get<MetricsSettings>()
                    ?? new MetricsSettings();

                if (!settings.Enabled)
                    return services;

                services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource.AddService(serviceName: settings.ServiceName))
                    .WithMetrics(metrics =>
                    {
                        metrics
                            // Request count, duration and status code for every endpoint.
                            .AddAspNetCoreInstrumentation()
                            // Outbound HTTP calls made by the API.
                            .AddHttpClientInstrumentation()
                            // GC, heap, thread pool and exception counters.
                            .AddRuntimeInstrumentation()
                            // Business instruments declared in the Application layer.
                            .AddMeter(RezerviseMetrics.MeterName);

                        if (settings.IncludeEfCoreInstrumentation)
                            metrics.AddMeter(EntityFrameworkCoreMeterName);

                        metrics.AddPrometheusExporter();
                    });
            }
            catch (Exception ex)
            {
                // A metrics misconfiguration must never prevent the API from starting.
                MetricsDiagnostics.WarnOnce($"Metrics pipeline disabled: {ex.Message}");
            }

            return services;
        }
    }
}
