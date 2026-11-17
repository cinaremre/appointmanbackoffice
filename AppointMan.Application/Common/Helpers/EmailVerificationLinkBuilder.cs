using AppointMan.Application.Common.Models;

namespace AppointMan.Application.Common.Helpers
{
    /// <summary>
    /// Dogrulama bagi adresini tek noktadan uretir; boylece controller ve
    /// handler ayni bicimi kullanir.
    /// </summary>
    public static class EmailVerificationLinkBuilder
    {
        /// <summary>
        /// Token'i temel adrese ekleyerek mutlak dogrulama bagi olusturur.
        /// </summary>
        /// <param name="baseUrl">Yapilandirilmis dogrulama ucunun tam adresi.</param>
        /// <param name="token">Ham (hash'lenmemis) dogrulama token'i.</param>
        /// <returns>Ornek: https://host/api/Auth/verify-email?token=abc</returns>
        /// <exception cref="InvalidOperationException">Temel adres bos oldugunda firlatilir.</exception>
        public static string Build(string baseUrl, string token)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Email:VerificationLinkBaseUrl yapilandirilmamis.");

            // Token "N" biciminde (32 hex) URL-safe; yine de savunmaci davranilir.
            var separator = baseUrl.Contains('?') ? '&' : '?';
            return $"{baseUrl}{separator}token={Uri.EscapeDataString(token)}";
        }

        /// <summary>
        /// E-posta govdesindeki bagi uygulama adresinden turetir; airac
        /// yapilandirma yoksa bos dize doner.
        /// </summary>
        /// <param name="baseUrl">Yapilandirilmis temel adres.</param>
        /// <returns>Ham token olmadan gorunen adres; yoksa bos dize.</returns>
        public static string DescribeEndpoint(string baseUrl)
            => string.IsNullOrWhiteSpace(baseUrl) ? string.Empty : baseUrl;
    }
}
