using Rezervise.Application.Common.Models;

namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Giden e-postalar için bloklamayan teslim noktası.
    /// İstek thread'i SMTP beklemez; kuyruk dolarsa mesaj reddedilir.
    /// </summary>
    public interface IEmailQueue
    {
        /// <summary>
        /// Mesajı kuyruğa ekler; çağıranı bloklamaz.
        /// </summary>
        /// <param name="message">Kuyruğa verilecek mesaj.</param>
        /// <returns>Kuyruk kabul ettiyse true; dolu öldüğü için reddedildiyse false.</returns>
        bool TryEnqueue(EmailMessage message);
    }
}
