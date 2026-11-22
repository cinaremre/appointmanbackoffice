using System.Security.Cryptography;
using System.Text;

namespace Rezervise.Application.Common.Security
{
    /// <summary>
    /// Hashes opaque refresh tokens before persistence, so a database leak cannot be replayed.
    /// The raw token only ever exists in transit and in the client's storage.
    /// </summary>
    public static class RefreshTokenHasher
    {
        /// <summary>
        /// Computes the Base64 encoded SHA-256 hash of the supplied token.
        /// </summary>
        /// <param name="rawToken">Plain refresh token issued to or received from the client.</param>
        /// <returns>Base64 encoded hash suitable for storage.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="rawToken"/> is null, empty or whitespace.</exception>
        public static string Hash(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                throw new ArgumentException("Refresh token must not be empty.", nameof(rawToken));

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Compares a raw refresh token with a stored hash using a fixed time comparison
        /// so token validity cannot be inferred from response timing.
        /// </summary>
        /// <param name="rawToken">Plain refresh token supplied by the caller.</param>
        /// <param name="storedHash">Base64 encoded hash previously persisted for the user.</param>
        /// <returns><c>true</c> when the token matches the stored hash.</returns>
        public static bool Matches(string? rawToken, string? storedHash)
        {
            if (string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            byte[] storedBytes;

            try
            {
                storedBytes = Convert.FromBase64String(storedHash);
            }
            catch (FormatException)
            {
                // A malformed hash can never match; treat it as a normal mismatch instead of failing loudly.
                return false;
            }

            var computedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

            return CryptographicOperations.FixedTimeEquals(computedBytes, storedBytes);
        }
    }
}
