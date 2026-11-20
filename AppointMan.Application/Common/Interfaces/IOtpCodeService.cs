using AppointMan.Domain.Entities;

namespace AppointMan.Application.Common.Interfaces
{
    /// <summary>
    /// Telefon dogrulama (OTP) kodunun uretimi ve dogrulanmasi.
    /// Ham kod yalnizca SMS govdesinde yer alir; veritabaninda hash'i saklanir.
    /// </summary>
    public interface IOtpCodeService
    {
        /// <summary>
        /// Yeni kod uretir, hash'ini kullaniciya yazar ve gonderilecek ham kodu dondurur.
        /// Sabit kod yapilandirilmissa o kod kullanilir.
        /// </summary>
        /// <param name="user">Kodu alacak kullanici.</param>
        /// <param name="utcNow">Islem zamani (UTC).</param>
        /// <returns>SMS ile gonderilecek ham kod.</returns>
        string IssueCode(User user, DateTime utcNow);

        /// <summary>
        /// Kodu dogrular; basarili oldugunda kullaniciyi telefon dogrulanmis sayar.
        /// </summary>
        /// <param name="user">Dogrulanacak kullanici.</param>
        /// <param name="code">Kullanici girdisi.</param>
        /// <param name="utcNow">Islem zamani (UTC).</param>
        /// <returns>Basarili mi ve basarisizsa hata anahtari.</returns>
        (bool Success, string? ErrorKey) VerifyCode(User user, string code, DateTime utcNow);
    }
}
