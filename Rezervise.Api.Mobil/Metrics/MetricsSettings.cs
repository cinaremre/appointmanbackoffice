namespace Rezervise.Api.Metrics
{
    /// <summary>
    /// Configuration for the metrics pipeline and the Prometheus scraping endpoint.
    /// </summary>
    public sealed class MetricsSettings
    {
        /// <summary>Configuration section name.</summary>
        public const string SectionName = "Metrics";

        /// <summary>
        /// When false the OpenTelemetry pipeline is not registered at all and the scraping
        /// endpoint is not mapped, so the process carries no metrics overhead.
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>Path where Prometheus scrapes the metrics document.</summary>
        public string EndpointPath { get; init; } = "/metrics";

        /// <summary>Service name reported through the OpenTelemetry resource.</summary>
        public string ServiceName { get; init; } = "Rezervise.Api";

        /// <summary>When true the EF Core meter is collected as well.</summary>
        public bool IncludeEfCoreInstrumentation { get; init; } = true;
    }
}
