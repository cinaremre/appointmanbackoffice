using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AppointMan.Application.Common.Services
{
    /// <summary>
    /// Issues access and refresh tokens from the validated <c>JwtSettings</c> configuration section.
    /// </summary>
    public class TokenService : ITokenService
    {
        private const string SectionName = "JwtSettings";
        private const int DefaultAccessTokenLifetimeMinutes = 60;
        private const int DefaultRefreshTokenLifetimeDays = 7;

        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initialises the service with the application configuration.
        /// </summary>
        /// <param name="configuration">Configuration root containing the JwtSettings section.</param>
        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public string GenerateAccessToken(User user)
        {
            var settings = ReadSettings();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.GivenName, $"{user.FirstName} {user.LastName}"),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                // Dil tercihi claim'i, kullanici bazli kultur saglayicisi tarafindan
                // DB sorgusu yapmadan okunur. Token uretildigi anda donar.
                new Claim("language", string.IsNullOrWhiteSpace(user.LanguageCode)
                    ? User.DefaultLanguageCode
                    : user.LanguageCode)
            };

            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                expires: GetAccessTokenExpiryUtc(),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <inheritdoc />
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <inheritdoc />
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var settings = ReadSettings();

            var tokenValidationParameters = new TokenValidationParameters
            {
                // Issuer and audience stay validated: an expired token from another
                // application must never be accepted during refresh.
                ValidateIssuer = true,
                ValidIssuer = settings.Issuer,
                ValidateAudience = true,
                ValidAudience = settings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)),
                // Lifetime is intentionally skipped: this method reads the identity of an already expired token.
                ValidateLifetime = false,
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
            };

            try
            {
                var principal = new JwtSecurityTokenHandler()
                    .ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch (SecurityTokenException)
            {
                // Malformed, tampered or wrongly signed tokens are simply not acceptable.
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public DateTime GetAccessTokenExpiryUtc()
            => DateTime.UtcNow.AddMinutes(GetAccessTokenLifetimeMinutes());

        /// <inheritdoc />
        public DateTime GetRefreshTokenExpiryUtc()
            => DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays());

        /// <summary>
        /// Reads the access token lifetime, falling back to the legacy ExpirationInMinutes value.
        /// </summary>
        /// <returns>Lifetime in minutes.</returns>
        private int GetAccessTokenLifetimeMinutes()
        {
            var section = _configuration.GetSection(SectionName);

            if (int.TryParse(section["AccessTokenLifetimeMinutes"], out var lifetime) && lifetime > 0)
                return lifetime;

            if (int.TryParse(section["ExpirationInMinutes"], out var legacyLifetime) && legacyLifetime > 0)
                return legacyLifetime;

            return DefaultAccessTokenLifetimeMinutes;
        }

        /// <summary>
        /// Reads the refresh token lifetime in days.
        /// </summary>
        /// <returns>Lifetime in days.</returns>
        private int GetRefreshTokenLifetimeDays()
        {
            var value = _configuration.GetSection(SectionName)["RefreshTokenLifetimeDays"];

            return int.TryParse(value, out var days) && days > 0
                ? days
                : DefaultRefreshTokenLifetimeDays;
        }

        /// <summary>
        /// Resolves the signing material once per call and fails loudly when it is missing.
        /// </summary>
        /// <returns>The resolved token settings.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the signing secret, issuer or audience is missing.</exception>
        private TokenSettings ReadSettings()
        {
            var section = _configuration.GetSection(SectionName);

            var secret = section["Secret"];
            var issuer = section["Issuer"];
            var audience = section["Audience"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException(
                    "JwtSettings:Secret is not configured. Supply it through user secrets or the JwtSettings__Secret environment variable.");
            }

            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException(
                    "JwtSettings:Issuer and JwtSettings:Audience must both be configured.");
            }

            return new TokenSettings(secret, issuer, audience);
        }

        /// <summary>Immutable snapshot of the signing material used for one operation.</summary>
        /// <param name="Secret">Symmetric signing secret.</param>
        /// <param name="Issuer">Token issuer.</param>
        /// <param name="Audience">Token audience.</param>
        private sealed record TokenSettings(string Secret, string Issuer, string Audience);
    }
}
