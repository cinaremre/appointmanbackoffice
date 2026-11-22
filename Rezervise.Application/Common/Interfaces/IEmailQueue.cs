using Rezervise.Application.Common.Models;

namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Giden e-postalar icin bloklamayan teslim noktasi.
    /// Istek thread'i SMTP beklemez; kuyruk dolarsa mesaj reddedilir.
    /// </summary>
    public interface IEmailQueue
    {
        /// <summary>
        /// Mesaji kuyruga ekler; cagirani bloklamaz.
        /// </summary>
        /// <param name="message">Kuyruga verilecek mesaj.</param>
        /// <returns>Kuyruk kabul ettiyse true; dolu oldugu icin reddedildiyse false.</returns>
        bool TryEnqueue(EmailMessage message);
    }
}
