using System.Text;

namespace AppointMan.Api.Configuration
{
    /// <summary>
    /// Startup guards that make the API fail fast when security critical configuration
    /// is missing, too weak, or unsafe for the current environment.
    /// </summary>
    public static class ConfigurationValidationExtensions
    {
        /// <summary>
        /// Registers <see cref="JwtSettings"/> with data annotation validation plus additional
        /// strength rules and forces validation during application startup.
        /// </summary>
        /// <param name="services">Target service collection.</param>
        /// <param name="configuration">Application configuration root.</param>
        /// <returns>The same service collection so calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
        public static IServiceCollection AddValidatedJwtSettings(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services
                .AddOptions<JwtSettings>()
                .Bind(configuration.GetSection(JwtSettings.SectionName))
                .ValidateDataAnnotations()
                // Character length is not enough: the key is used as raw UTF-8 bytes for HMAC-SHA256.
                .Validate(
                    settings => Encoding.UTF8.GetByteCount(settings.Secret) >= JwtSettings.MinimumSecretByteLength,
                    $"JwtSettings:Secret must be at least {JwtSettings.MinimumSecretByteLength} bytes of UTF-8 data.")
                .Validate(
                    settings => Uri.TryCreate(settings.Issuer, UriKind.Absolute, out _),
                    "JwtSettings:Issuer must be an absolute URI so issuer validation is meaningful.")
                .Validate(
                    settings => !string.IsNullOrWhiteSpace(settings.Audience),
                    "JwtSettings:Audience is required so audience validation is meaningful.")
                .ValidateOnStart();

            return services;
        }

        /// <summary>
        /// Returns the configured connection string, throwing an actionable error when it is missing.
        /// </summary>
        /// <param name="configuration">Application configuration root.</param>
        /// <param name="name">Connection string name; defaults to <c>DefaultConnection</c>.</param>
        /// <returns>The resolved connection string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the connection string is missing or blank.</exception>
        public static string GetRequiredConnectionString(this IConfiguration configuration, string name = "DefaultConnection")
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var connectionString = configuration.GetConnectionString(name);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{name}' is not configured. Supply it through the " +
                    $"ConnectionStrings__{name} environment variable or 'dotnet user-secrets'. " +
                    "Secrets must never be committed to appsettings.json.");
            }

            return connectionString;
        }

        /// <summary>
        /// Rejects the wildcard <c>AllowedHosts</c> value outside the Development environment,
        /// which would otherwise leave the API open to host header injection.
        /// </summary>
        /// <param name="configuration">Application configuration root.</param>
        /// <param name="environment">Current hosting environment.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the allowed hosts value is a wildcard outside Development.</exception>
        public static void ValidateAllowedHosts(this IConfiguration configuration, IHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(environment);

            if (environment.IsDevelopment())
            {
                return;
            }

            var allowedHosts = configuration["AllowedHosts"];

            if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
            {
                throw new InvalidOperationException(
                    $"AllowedHosts must be an explicit host list when ASPNETCORE_ENVIRONMENT is " +
                    $"'{environment.EnvironmentName}'. Configure AllowedHosts with the concrete public host names of the API.");
            }
        }
    }
}
