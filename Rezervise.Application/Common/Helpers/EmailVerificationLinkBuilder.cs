using Rezervise.Application.Common.Models;

namespace Rezervise.Application.Common.Helpers
{
    /// <summary>
    /// Doğrulama bağı adresini tek noktadan üretir; böylece controller ve
    /// handler aynı biçimi kullanır.
    /// </summary>
    public static class EmailVerificationLinkBuilder
    {
        /// <summary>
        /// Token'i temel adrese ekleyerek mutlak doğrulama bağı oluşturur.
        /// </summary>
        /// <param name="baseUrl">Yapılandırılmış doğrulama üçünün tam adresi.</param>
        /// <param name="token">Ham (hash'lenmemiş) doğrulama token'i.</param>
        /// <returns>Örnek: https://hoşt/api/Auth/verify-email?token=abc</returns>
        /// <exception cref="InvalidOperationException">Temel adres boş öldüğünda fırlatılır.</exception>
        public static string Build(string baseUrl, string token)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Email:VerificationLinkBaseUrl yapılandırılmamış.");

            // Token "N" biçiminde (32 hex) URL-safe; yine de savunmacı davranılır.
            var separator = baseUrl.Contains('?') ? '&' : '?';
            return $"{baseUrl}{separator}token={Uri.EscapeDataString(token)}";
        }

        /// <summary>
        /// E-posta gövdesindeki bağı uygulama adresinden türetir; airac
        /// yapılandırma yoksa boş dize döner.
        /// </summary>
        /// <param name="baseUrl">Yapılandırılmış temel adres.</param>
        /// <returns>Ham token olmadan görünen adres; yoksa boş dize.</returns>
        public static string DescribeEndpoint(string baseUrl)
            => string.IsNullOrWhiteSpace(baseUrl) ? string.Empty : baseUrl;
    }
}
