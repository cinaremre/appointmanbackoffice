using System.Security.Cryptography;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Application.Common.Security;
using Rezervise.Domain.Entities;

namespace Rezervise.Infrastructure.Services.Sms
{
    /// <summary>
    /// Telefon dogrulama kodunu uretir ve dogrular. Ham kod saklanmaz;
    /// e-posta dogrulama token'i ile ayni hash deseni (SHA-256) kullanilir.
    /// </summary>
    public sealed class OtpCodeService : IOtpCodeService
    {
        private readonly OtpOptions _options;

        /// <summary>
        /// Servisi yapilandirma ile olusturur.
        /// </summary>
        /// <param name="options">OTP ayarlari.</param>
        /// <exception cref="ArgumentNullException">Ayarlar null oldugunda firlatilir.</exception>
        public OtpCodeService(OtpOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public string IssueCode(User user, DateTime utcNow)
        {
            ArgumentNullException.ThrowIfNull(user);

            // Saglayici baglanana kadar sabit kod kullanilabilir; yapilandirilmamissa rastgele uretilir.
            var code = _options.HasFixedCode ? _options.FixedCode! : GenerateCode(_options.CodeLength);

            user.IssuePhoneVerificationCode(
                RefreshTokenHasher.Hash(code),
                utcNow.AddMinutes(_options.LifetimeMinutes),
                utcNow);

            return code;
        }

        /// <inheritdoc />
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
            // Kriptografik rastgelelik: tahmin edilebilir kod uretilmez.
            var max = (int)Math.Pow(10, Math.Clamp(length, 4, 8));
            var value = RandomNumberGenerator.GetInt32(0, max);

            return value.ToString(new string('0', Math.Clamp(length, 4, 8)));
        }
    }
}
