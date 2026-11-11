namespace AppointMan.Api.Configuration
{
    /// <summary>
    /// CORS configuration based on an explicit origin allow-list.
    /// Wildcards combined with credentials are never allowed.
    /// </summary>
    public sealed class CorsSettings
    {
        /// <summary>Configuration section name.</summary>
        public const string SectionName = "Cors";

        /// <summary>Policy name registered on the service collection.</summary>
        public const string PolicyName = "AppointManApi";

        /// <summary>Exact origins allowed to call the API.</summary>
        public IReadOnlyList<string> AllowedOrigins { get; init; } = Array.Empty<string>();

        /// <summary>Request headers accepted from browsers.</summary>
        public IReadOnlyList<string> AllowedHeaders { get; init; } =
            new[] { "Authorization", "Content-Type", "Accept", "X-Correlation-Id" };

        /// <summary>HTTP methods accepted from browsers.</summary>
        public IReadOnlyList<string> AllowedMethods { get; init; } =
            new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS" };
    }
}
