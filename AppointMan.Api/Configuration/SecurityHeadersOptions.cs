namespace AppointMan.Api.Configuration
{
    /// <summary>
    /// Configurable browser hardening headers applied to every API response.
    /// An API only returns JSON, so the policy can stay maximally restrictive.
    /// </summary>
    public sealed class SecurityHeadersOptions
    {
        /// <summary>Configuration section name.</summary>
        public const string SectionName = "SecurityHeaders";

        /// <summary>Content-Security-Policy value.</summary>
        public string ContentSecurityPolicy { get; init; } = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";

        /// <summary>X-Frame-Options value; prevents clickjacking.</summary>
        public string FrameOptions { get; init; } = "DENY";

        /// <summary>Referrer-Policy value; prevents leaking URLs to third parties.</summary>
        public string ReferrerPolicy { get; init; } = "no-referrer";

        /// <summary>Permissions-Policy value disabling powerful browser features.</summary>
        public string PermissionsPolicy { get; init; } = "camera=(), microphone=(), geolocation=(), payment=()";

        /// <summary>
        /// Path prefixes whose responses must never be cached. Authentication and
        /// user endpoints are always included by the middleware.
        /// </summary>
        public IReadOnlyList<string> NoStorePathPrefixes { get; init; } = Array.Empty<string>();
    }
}
