using AppointMan.Application.Common.Models;

namespace AppointMan.Application.Common.Interfaces
{
    /// <summary>
    /// E-posta gonderim soyutlamasi. Uygulamalar tasima hatalarinda istisna
    /// firlatmaz; hatayi loglar ve yutar ki cagiran akis etkilenmesin.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Mesaji gonderir. Tasima hatasi olursa istisna firlatilmaz.
        /// </summary>
        /// <param name="message">Gonderilecek mesaj.</param>
        /// <param name="cancellationToken">Iptal belirteci.</param>
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }
}
