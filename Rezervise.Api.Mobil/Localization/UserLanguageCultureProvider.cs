using System.Security.Claims;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Logging;

namespace Rezervise.Api.Localization
{
    /// <summary>
    /// Oturum açmış kullanıcı için dil tercihini belirler.
    /// Önce token içindeki <c>language</c> claim'ine bakar (hızlı yol, DB sorgusu yok);
    /// claim yoksa (örneğin eski token) kullanıcı kaydındaki <c>LanguageCode</c> okunur.
    /// Anonim isteklerde null döner, böylece sıradaki provider (Accept-Language) devreye girer.
    /// </summary>
    public sealed class UserLanguageCultureProvider : IRequestCultureProvider
    {
        /// <summary>Token içinde dil tercihini taşıyan claim adı.</summary>
        public const string LanguageClaimType = "language";

        private static readonly string[] SupportedLanguageCodes = { "tr", "en" };

        /// <inheritdoç />
        public async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
        {
            try
            {
                var user = httpContext.User;

                // Anonim istek: karar verme, sonraki provider'a bırak.
                if (user?.Identity?.IsAuthenticated != true)
                    return null;

                // 1. Hızlı yol: token içindeki dil claim'i.
                var claimLanguage = Normalize(user.FindFirst(LanguageClaimType)?.Value);
                if (claimLanguage is not null)
                    return new ProviderCultureResult(claimLanguage);

                // 2. Yedek yol: claim yoksa kullanıcı kaydından oku.
                var userIdValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? user.FindFirst("userId")?.Value;

                if (!Guid.TryParse(userIdValue, out var userId))
                    return null;

                var repository = httpContext.RequestServices.GetService(typeof(IRepository<User>)) as IRepository<User>;
                if (repository is null)
                    return null;

                var users = await repository.FindAsync(
                    u => u.Id == userId && !u.IsDeleted,
                    httpContext.RequestAborted);

                var databaseLanguage = Normalize(users.FirstOrDefault()?.LanguageCode);

                return databaseLanguage is null ? null : new ProviderCultureResult(databaseLanguage);
            }
            catch (Exception ex)
            {
                // Kültür çözümleme hatası isteği bloklamaz; header tabanlı tespite düşülür.
                var loggerFactory = httpContext.RequestServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
                loggerFactory?
                    .CreateLogger<UserLanguageCultureProvider>()
                    .LogWarning(ex, "Kullanıcı dil tercihi okunamadı, Accept-Language tespitine düşüldü.");

                return null;
            }
        }

        /// <summary>
        /// Dil kodunu iki harfli küçük forma indirger; desteklenmiyorsa null döner.
        /// </summary>
        /// <param name="value">Ham dil kodu.</param>
        /// <returns>Desteklenen kod ("tr"/"en") veya null.</returns>
        private static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var code = value.Trim().ToLowerInvariant();

            if (code.Length > 2)
                code = code[..2];

            return SupportedLanguageCodes.Contains(code) ? code : null;
        }
    }
}
