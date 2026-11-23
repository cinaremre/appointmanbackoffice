using System.Security.Cryptography;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Application.Common.Security;
using Rezervise.Domain.Entities;

namespace Rezervise.Infrastructure.Services.Sms
{
    /// <summary>
    /// Telefon doğrulama kodunu üretir ve doğrular. Ham kod saklanmaz;
    /// e-posta doğrulama token'i ile aynı hash deseni (SHA-256) kullanılır.
    /// </summary>
    public sealed class OtpCodeService : IOtpCodeService
    {
        private readonly OtpOptions _options;

        /// <summary>
        /// Servisi yapılandırma ile oluşturur.
        /// </summary>
        /// <param name="options">OTP ayarları.</param>
        /// <exception cref="ArgumentNullException">Ayarlar null öldüğünda fırlatılır.</exception>
        public OtpCodeService(OtpOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoç />
        public string IssueCode(User user, DateTime utcNow)
        {
            ArgumentNullException.ThrowIfNull(user);

            // Sağlayıcı bağlanana kadar sabit kod kullanılabilir; yapılandırılmamışsa rastgele üretilir.
            var code = _options.HasFixedCode ? _options.FixedCode! : GenerateCode(_options.CodeLength);

            user.IssuePhoneVerificationCode(
                RefreshTokenHasher.Hash(code),
                utcNow.AddMinutes(_options.LifetimeMinutes),
                utcNow);

            return code;
        }

        /// <inheritdoç />
        public (bool Success, string? ErrorKey) VerifyCode(User user, string code, DateTime utcNow)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (user.IsPhoneVerified)
                return (true, null);

            if (string.IsNullOrWhiteSpace(code))
                return (false, "Otp.CodeRequired");

            if (string.IsNullOrWhiteSpace(user.PhoneVerificationCodeHash))
                return (false, "Otp.CodeNotIssued");

            if (user.PhoneVerificationCodeExpiresAt is null || user.PhoneVerificationCodeExpiresAt < utcNow)
                return (false, "Otp.Expired");

            if (user.PhoneVerificationAttemptCount >= _options.MaxAttempts)
                return (false, "Otp.TooManyAttempts");

            var providedHash = RefreshTokenHasher.Hash(code.Trim());

            if (!string.Equals(user.PhoneVerificationCodeHash, providedHash, StringComparison.Ordinal))
            {
                user.IncrementPhoneVerificationAttempt();
                return (false, "Otp.InvalidCode");
            }

            user.VerifyPhone();
            user.ClearPhoneVerificationCode();

            return (true, null);
        }

        private static string GenerateCode(int length)
        {
            // Kriptografik rastgelelik: tahmin edilebilir kod üretilmez.
            var max = (int)Math.Pow(10, Math.Clamp(length, 4, 8));
            var value = RandomNumberGenerator.GetInt32(0, max);

            return value.ToString(new string('0', Math.Clamp(length, 4, 8)));
        }
    }
}
