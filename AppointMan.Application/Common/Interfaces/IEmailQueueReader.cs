using AppointMan.Application.Common.Models;

namespace AppointMan.Application.Common.Interfaces
{
    /// <summary>
    /// Kuyrugun tuketici tarafi. Yalnizca arka plan gonderim servisi kullanir;
    /// uretici handler'lar <see cref="IEmailQueue"/> uzerinden yazar.
    /// </summary>
    public interface IEmailQueueReader
    {
        /// <summary>
        /// Kuyruktaki mesajlari geldikce akitir; iptal edilene kadar bekler.
        /// </summary>
        /// <param name="cancellationToken">Iptal belirteci.</param>
        /// <returns>Mesaj akisi.</returns>
        IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken cancellationToken = default);
    }
}
