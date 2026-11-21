using System.ComponentModel.DataAnnotations;

namespace AppointMan.BackOffice.Api.Configuration
{
    /// <summary>
    /// Backoffice JWT ayarlari. Ayri audience kullanilir; musteri API'si
    /// tarafindan uretilen token'lar backoffice'te GECERLI OLMAZ.
    /// </summary>
    public sealed class BackOfficeJwtSettings
    {
        /// <summary>Configuration section name.</summary>
        public const string SectionName = "JwtSettings";

        /// <summary>Minimum required byte length for the symmetric signing secret.</summary>
        public const int MinimumSecretByteLength = 32;

        /// <summary>Symmetric signing secret. En az 32 karakter; secret store'dan gelir.</summary>
        [Required(ErrorMessage = "JwtSettings:Secret is required.")]
        [MinLength(MinimumSecretByteLength, ErrorMessage = "JwtSettings:Secret must be at least 32 characters long.")]
        public string Secret { get; init; } = string.Empty;

        /// <summary>Token issuer.</summary>
        [Required(ErrorMessage = "JwtSettings:Issuer is required.")]
        public string Issuer { get; init; } = string.Empty;

        /// <summary>Backoffice'e ozel audience ("AppointMan-Backoffice").</summary>
        [Required(ErrorMessage = "JwtSettings:Audience is required.")]
        public string Audience { get; init; } = "AppointMan-Backoffice";

        /// <summary>Clock skew tolerated during token validation, in seconds.</summary>
        [Range(0, 300)]
        public int ClockSkewSeconds { get; init; }

        /// <summary>When true, token metadata must be served over HTTPS only.</summary>
        public bool RequireHttpsMetadata { get; init; } = true;

        /// <summary>Clock skew as a <see cref="TimeSpan"/>.</summary>
        public TimeSpan ClockSkew => TimeSpan.FromSeconds(ClockSkewSeconds);
    }
}
