using System.Security.Claims;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Domain.Entities;

namespace Rezervise.Tests.Fakes;

/// <summary>
/// Belirleyici (deterministik) token ureten test cifti. Gercek JWT uretimi
/// testlerde gereksiz bagimlilik oldugu icin yalnizca sozlesme taklit edilir.
/// </summary>
public sealed class FakeTokenService : ITokenService
{
    /// <summary>Uretilen access token degeri.</summary>
    public const string AccessToken = "test-access-token";

    /// <summary>Uretilen refresh token degeri.</summary>
    public const string RefreshToken = "test-refresh-token";

    public string GenerateAccessToken(User user) => $"{AccessToken}-{user.Id:N}";

    public string GenerateRefreshToken() => RefreshToken;

    /// <summary>
    /// Suresi dolmus token'dan kimlik okuma. Testlerde gercek imza dogrulamasi
    /// yapilmaz; sozlesme geregi null donulur.
    /// </summary>
    /// <param name="token">Suresi dolmus access token.</param>
    /// <returns>Her zaman null.</returns>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token) => null;

    public DateTime GetAccessTokenExpiryUtc() => new(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);

    public DateTime GetRefreshTokenExpiryUtc() => new(2026, 11, 30, 12, 0, 0, DateTimeKind.Utc);
}
