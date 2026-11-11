using System.Security.Claims;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Logging;

namespace AppointMan.Api.Localization
{
    /// <summary>
    /// Oturum acmis kullanici icin dil tercihini belirler.
    /// Once token icindeki <c>language</c> claim'ine bakar (hizli yol, DB sorgusu yok);
    /// claim yoksa (ornegin eski token) kullanici kaydindaki <c>LanguageCode</c> okunur.
    /// Anonim isteklerde null doner, boylece siradaki provider (Accept-Language) devreye girer.
    /// </summary>
    public sealed class UserLanguageCultureProvider : IRequestCultureProvider
    {
        /// <summary>Token icinde dil tercihini tasiyan claim adi.</summary>
        public const string LanguageClaimType = "language";

        private static readonly string[] SupportedLanguageCodes = { "tr", "en" };

        /// <inheritdoc />
        public async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
        {
            try
            {
                var user = httpContext.User;

                // Anonim istek: karar verme, sonraki provider'a birak.
                if (user?.Identity?.IsAuthenticated != true)
                    return null;

                // 1. Hizli yol: token icindeki dil claim'i.
                var claimLanguage = Normalize(user.FindFirst(LanguageClaimType)?.Value);
                if (claimLanguage is not null)
                    return new ProviderCultureResult(claimLanguage);

                // 2. Yedek yol: claim yoksa kullanici kaydindan oku.
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
                // Kultur cozumleme hatasi istegi bloklamaz; header tabanli tespite dusulur.
                var loggerFactory = httpContext.RequestServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
                loggerFactory?
                    .CreateLogger<UserLanguageCultureProvider>()
                    .LogWarning(ex, "Kullanici dil tercihi okunamadi, Accept-Language tespitine dusuldu.");

                return null;
            }
        }

        /// <summary>
        /// Dil kodunu iki harfli kucuk forma indirger; desteklenmiyorsa null doner.
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
