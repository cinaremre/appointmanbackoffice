using System;
using System.Threading;
using System.Threading.Tasks;

namespace AppointMan.Application.Common.Abstractions
{
    /// <summary>
    /// Isletme puan ve yorum sayaci alanlarini tek SQL ifadesiyle gunceller.
    /// Read-modify-write deseni yerine atomik guncelleme kullanildigi icin
    /// es zamanli yorum ekleme/silme islemlerinde sayac kaybi (lost update) olusmaz.
    /// </summary>
    public interface IBusinessRatingUpdater
    {
        /// <summary>
        /// Yeni bir puan eklendiginde toplam yorum sayisini ve ortalama puani
        /// tek bir UPDATE ifadesiyle gunceller.
        /// </summary>
        /// <param name="businessId">Isletme kimligi.</param>
        /// <param name="score">Eklenen puan (1-5).</param>
        /// <param name="cancellationToken">Iptal tokeni.</param>
        /// <returns>Guncellenen satir sayisi.</returns>
        Task<int> UpdateOnNewReviewAsync(Guid businessId, int score, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bir puan silindiginde toplam yorum sayisini ve ortalama puani
        /// tek bir UPDATE ifadesiyle gunceller.
        /// </summary>
        /// <param name="businessId">Isletme kimligi.</param>
        /// <param name="score">Silinen puan (1-5).</param>
        /// <param name="cancellationToken">Iptal tokeni.</param>
        /// <returns>Guncellenen satir sayisi.</returns>
        Task<int> UpdateOnReviewRemovedAsync(Guid businessId, int score, CancellationToken cancellationToken = default);

        /// <summary>
        /// Yorum guncelleme, silme ve gorunurluk degisikliklerinden sonra
        /// puan ve sayaci gorunur yorumlardan tek SQL ifadesiyle yeniden hesaplar.
        /// </summary>
        /// <param name="businessId">Isletme kimligi.</param>
        /// <param name="cancellationToken">Iptal tokeni.</param>
        /// <returns>Guncellenen satir sayisi.</returns>
        Task<int> RecalculateAsync(Guid businessId, CancellationToken cancellationToken = default);
    }
}
