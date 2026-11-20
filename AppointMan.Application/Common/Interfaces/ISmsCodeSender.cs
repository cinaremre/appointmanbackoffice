namespace AppointMan.Application.Common.Interfaces
{
    /// <summary>
    /// Telefon dogrulama kodunu SMS ile gonderir. Saglayici baglanana kadar
    /// log tabanli implementasyon kullanilir; is akisi saglayicidan bagimsizdir.
    /// </summary>
    public interface ISmsCodeSender
    {
        /// <summary>
        /// Kodu belirtilen numaraya gonderir.
        /// </summary>
        /// <param name="phoneNumber">Alici numarasi (E.164 / +90... formati).</param>
        /// <param name="code">Gonderilecek dogrulama kodu.</param>
        /// <param name="cancellationToken">Iptal token'i.</param>
        /// <returns>Gonderim tamamlandiginda tamamlanan gorev.</returns>
        Task SendAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
    }
}
