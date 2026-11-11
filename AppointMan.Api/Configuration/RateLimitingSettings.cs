using System.ComponentModel.DataAnnotations;

namespace AppointMan.Api.Configuration
{
    /// <summary>
    /// Rate limiting configuration. Authentication endpoints get a much stricter budget
    /// than regular API traffic to blunt brute force and credential stuffing attacks.
    /// </summary>
    public sealed class RateLimitingSettings
    {
        /// <summary>Configuration section name.</summary>
        public const string SectionName = "RateLimiting";

        /// <summary>Permit limit per window for regular API traffic.</summary>
        [Range(1, 10000)]
        public int GlobalPermitLimit { get; init; } = 100;

        /// <summary>Window length in seconds for regular API traffic.</summary>
        [Range(1, 3600)]
        public int GlobalWindowSeconds { get; init; } = 60;

        /// <summary>Permit limit per window for authentication endpoints.</summary>
        [Range(1, 100)]
        public int AuthPermitLimit { get; init; } = 5;

        /// <summary>Window length in seconds for authentication endpoints.</summary>
        [Range(1, 3600)]
        public int AuthWindowSeconds { get; init; } = 60;
    }
}
