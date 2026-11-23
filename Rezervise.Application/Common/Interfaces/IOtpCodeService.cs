using Rezervise.Domain.Entities;

namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Telefon doğrulama (OTP) kodunun üretimi ve doğrulanması.
    /// Ham kod yalnızca SMS gövdesinde yer alır; veritabanında hash'i saklanır.
    /// </summary>
    public interface IOtpCodeService
    {
        /// <summary>
        /// Yeni kod üretir, hash'ini kullanıcıya yazar ve gönderilecek ham kodu döndürür.
        /// Sabit kod yapılandırılmışsa o kod kullanılır.
        /// </summary>
        /// <param name="user">Kodu alacak kullanıcı.</param>
        /// <param name="utcNow">İşlem zamanı (UTC).</param>
        /// <returns>SMS ile gönderilecek ham kod.</returns>
        string IssueCode(User user, DateTime utcNow);

        /// <summary>
        /// Kodu doğrular; başarılı öldüğünda kullanıcıyı telefon doğrulanmış sayar.
        /// </summary>
        /// <param name="user">Doğrulanacak kullanıcı.</param>
        /// <param name="code">Kullanıcı girdisi.</param>
        /// <param name="utcNow">İşlem zamanı (UTC).</param>
        /// <returns>Başarılı mi ve başarısızsa hata anahtarı.</returns>
        (bool Success, string? ErrorKey) VerifyCode(User user, string code, DateTime utcNow);
    }
}
