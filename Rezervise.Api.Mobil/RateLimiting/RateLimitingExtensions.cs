using System.Threading.RateLimiting;
using Rezervise.Api.Configuration;
using Rezervise.Api.Metrics;

namespace Rezervise.Api.RateLimiting
{
    /// <summary>
    /// Registers the partitioned rate limiter used by the API pipeline.
    /// </summary>
    public static class RateLimitingExtensions
    {
        private const string AuthenticationPathPrefix = "/api/auth";

        /// <summary>Path used when configuration does not override the metrics endpoint.</summary>
        private const string DefaultMetricsPath = "/metrics";

        /// <summary>
        /// Adds a global rate limiter that partitions by authenticated user (or client IP)
        /// and applies a stricter budget to authentication endpoints.
        /// </summary>
        /// <param name="services">Target service collection.</param>
        /// <param name="configuration">Configuration used to bind <see cref="RateLimitingSettings"/>.</param>
        /// <returns>The same service collection so calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
        public static IServiceCollection AddApiRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            var settings = configuration.GetSection(RateLimitingSettings.SectionName).Get<RateLimitingSettings>()
                ?? new RateLimitingSettings();

            services.Configure<RateLimitingSettings>(configuration.GetSection(RateLimitingSettings.SectionName));

            // Prometheus scrapes every few seconds; throttling it would create gaps in the dashboards.
            var metricsPath = configuration.GetSection(MetricsSettings.SectionName)["EndpointPath"];

            if (string.IsNullOrWhiteSpace(metricsPath))
                metricsPath = DefaultMetricsPath;

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                    context.HttpContext.Response.ContentType = "application/problem+json";

                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        type = "https://tools.ietf.org/html/rfç6585#section-4",
                        title = "Too many requests. Please retry later.",
                        status = StatusCodes.Status429TooManyRequests,
                        correlationId = context.HttpContext.TraceIdentifier
                    }, cancellationToken);
                };

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    // The scraping endpoint is exempt: rate limiting it would drop metric samples.
                    if (httpContext.Request.Path.StartsWithSegments(metricsPath, StringComparison.OrdinalIgnoreCase))
                        return RateLimitPartition.GetNoLimiter("metrics");

                    var isAuthenticationRequest = httpContext.Request.Path
                        .StartsWithSegments(AuthenticationPathPrefix, StringComparison.OrdinalIgnoreCase);

                    var permitLimit = isAuthenticationRequest
                        ? settings.AuthPermitLimit
                        : settings.GlobalPermitLimit;

                    var window = TimeSpan.FromSeconds(
                        isAuthenticationRequest ? settings.AuthWindowSeconds : settings.GlobalWindowSeconds);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"{ResolvePartitionKey(httpContext)}:{isAuthenticationRequest}",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = window,
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });
            });

            return services;
        }

        /// <summary>
        /// Builds the partition key: authenticated users are limited per identity,
        /// anonymous callers per client IP address.
        /// </summary>
        /// <param name="httpContext">Current HTTP context.</param>
        /// <returns>The partition key.</returns>
        private static string ResolvePartitionKey(HttpContext httpContext)
        {
            var userId = httpContext.User?.FindFirst("userId")?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
                return $"user:{userId}";

            return $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        }
    }
}
