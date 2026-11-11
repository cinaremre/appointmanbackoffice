using System.ComponentModel.DataAnnotations;

namespace AppointMan.Api.Configuration
{
    /// <summary>
    /// Strongly typed JWT configuration bound from the "JwtSettings" section.
    /// The signing secret is never stored in appsettings.json; it must be supplied
    /// through user secrets, environment variables or a dedicated secret store.
    /// </summary>
    public sealed class JwtSettings
    {
        /// <summary>Configuration section name.</summary>
        public const string SectionName = "JwtSettings";

        /// <summary>Minimum required byte length for the symmetric signing secret.</summary>
        public const int MinimumSecretByteLength = 32;

        /// <summary>Upper bound accepted for the access token lifetime, in minutes.</summary>
        public const int MaxAccessTokenLifetimeMinutes = 240;

        /// <summary>Symmetric signing secret. Must provide at least 32 bytes of UTF-8 data.</summary>
        [Required(ErrorMessage = "JwtSettings:Secret is required.")]
        [MinLength(MinimumSecretByteLength, ErrorMessage = "JwtSettings:Secret must be at least 32 characters long.")]
        public string Secret { get; init; } = string.Empty;

        /// <summary>Token issuer written to and validated from the "iss" claim.</summary>
        [Required(ErrorMessage = "JwtSettings:Issuer is required.")]
        public string Issuer { get; init; } = string.Empty;

        /// <summary>Token audience written to and validated from the "aud" claim.</summary>
        [Required(ErrorMessage = "JwtSettings:Audience is required.")]
        public string Audience { get; init; } = string.Empty;

        /// <summary>Access token lifetime in minutes, kept for backwards compatibility with the token service.</summary>
        [Range(1, MaxAccessTokenLifetimeMinutes)]
        public int ExpirationInMinutes { get; init; } = 60;

        /// <summary>Access token lifetime in minutes exposed to callers.</summary>
        [Range(1, MaxAccessTokenLifetimeMinutes)]
        public int AccessTokenLifetimeMinutes { get; init; } = 60;

        /// <summary>Refresh token lifetime in days.</summary>
        [Range(1, 90)]
        public int RefreshTokenLifetimeDays { get; init; } = 7;

        /// <summary>Clock skew tolerated during token validation, in seconds.</summary>
        [Range(0, 300)]
        public int ClockSkewSeconds { get; init; }

        /// <summary>When true, token metadata must be served over HTTPS only.</summary>
        public bool RequireHttpsMetadata { get; init; } = true;

        /// <summary>Clock skew expressed as a <see cref="TimeSpan"/> for token validation parameters.</summary>
        public TimeSpan ClockSkew => TimeSpan.FromSeconds(ClockSkewSeconds);
    }
}
