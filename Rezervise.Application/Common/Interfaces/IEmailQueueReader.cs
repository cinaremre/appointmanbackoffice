using Rezervise.Application.Common.Models;

namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Kuyruğun tüketici tarafı. Yalnızca arka plan gönderim servisi kullanır;
    /// üretici handler'lar <see cref="IEmailQueue"/> üzerinden yazar.
    /// </summary>
    public interface IEmailQueueReader
    {
        /// <summary>
        /// Kuyruktaki mesajları geldikçe akıtır; iptal edilene kadar bekler.
        /// </summary>
        /// <param name="cancellationToken">İptal belirteci.</param>
        /// <returns>Mesaj akışı.</returns>
        IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken cancellationToken = default);
    }
}
