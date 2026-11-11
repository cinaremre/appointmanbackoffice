using AppointMan.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AppointMan.Api.Middleware
{
    /// <summary>
    /// Applies HTTP hardening headers (CSP, nosniff, frame options, referrer and permissions policy)
    /// to every response, and prevents caching of sensitive endpoints.
    /// </summary>
    public sealed class SecurityHeadersMiddleware
    {
        private static readonly string[] AlwaysNoStorePrefixes = { "/api/auth", "/api/user" };

        private readonly RequestDelegate _next;
        private readonly SecurityHeadersOptions _options;

        /// <summary>
        /// Initialises the middleware.
        /// </summary>
        /// <param name="next">Next delegate in the pipeline.</param>
        /// <param name="options">Header configuration.</param>
        public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
        {
            _next = next;
            _options = options.Value;
        }

        /// <summary>
        /// Adds the configured security headers and continues the pipeline.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        /// <returns>A task representing the middleware execution.</returns>
        public Task InvokeAsync(HttpContext context)
        {
            // Run on response start so headers are written even for error responses.
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                headers.TryAdd("X-Content-Type-Options", "nosniff");
                headers.TryAdd("X-Frame-Options", _options.FrameOptions);
                headers.TryAdd("Referrer-Policy", _options.ReferrerPolicy);
                headers.TryAdd("Permissions-Policy", _options.PermissionsPolicy);
                headers.TryAdd("Content-Security-Policy", _options.ContentSecurityPolicy);
                headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");

                if (ShouldDisableCache(context.Request.Path))
                {
                    headers.TryAdd("Cache-Control", "no-store, no-cache, must-revalidate");
                    headers.TryAdd("Pragma", "no-cache");
                }

                return Task.CompletedTask;
            });

            return _next(context);
        }

        /// <summary>
        /// Determines whether the response for the given path must not be cached.
        /// </summary>
        /// <param name="path">Request path.</param>
        /// <returns><c>true</c> when caching must be disabled.</returns>
        private bool ShouldDisableCache(PathString path)
        {
            if (!path.HasValue)
                return false;

            foreach (var prefix in AlwaysNoStorePrefixes)
            {
                if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            foreach (var prefix in _options.NoStorePathPrefixes)
            {
                if (!string.IsNullOrWhiteSpace(prefix)
                    && path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
