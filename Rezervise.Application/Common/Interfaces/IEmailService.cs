using Rezervise.Application.Common.Models;

namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// E-posta gönderim soyutlaması. Uygulamalar taşıma hatalarında istisna
    /// fırlatmaz; hatayı loglar ve yutar ki çağıran akış etkilenmesin.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Mesajı gönderir. Taşıma hatası olursa istisna fırlatılmaz.
        /// </summary>
        /// <param name="message">Gönderilecek mesaj.</param>
        /// <param name="cancellationToken">İptal belirteci.</param>
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }
}
