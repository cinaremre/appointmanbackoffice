using System.Security.Claims;
using Rezervise.Domain.Entities;

namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Issues and parses JSON Web Tokens. Persistence of refresh token state is the
    /// responsibility of the calling handler; this service only performs cryptographic work.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Creates a signed access token for the supplied user.
        /// </summary>
        /// <param name="user">User whose identity and role are embedded in the token.</param>
        /// <returns>The serialized JWT.</returns>
        string GenerateAccessToken(User user);

        /// <summary>
        /// Creates a cryptographically random opaque refresh token.
        /// Only its hash must be persisted.
        /// </summary>
        /// <returns>A Base64 encoded 64 byte random token.</returns>
        string GenerateRefreshToken();

        /// <summary>
        /// Reads the identity from an access token whose lifetime has already expired.
        /// Issuer, audience and signature are still validated.
        /// </summary>
        /// <param name="token">Expired access token.</param>
        /// <returns>The principal, or <c>null</c> when the token is not acceptable.</returns>
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

        /// <summary>Absolute expiry applied to newly issued access tokens.</summary>
        /// <returns>Expiry timestamp in UTC.</returns>
        DateTime GetAccessTokenExpiryUtc();

        /// <summary>Absolute expiry applied to newly issued refresh tokens.</summary>
        /// <returns>Expiry timestamp in UTC.</returns>
        DateTime GetRefreshTokenExpiryUtc();
    }
}
