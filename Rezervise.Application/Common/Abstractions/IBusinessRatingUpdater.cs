using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rezervise.Application.Common.Abstractions
{
    /// <summary>
    /// İşletme puan ve yorum sayacı alanlarını tek SQL ifadesiyle günceller.
    /// Read-modify-write deseni yerine atomik güncelleme kullanıldığı için
    /// es zamanlı yorum ekleme/silme işlemlerinde sayaç kaybı (lost update) oluşmaz.
    /// </summary>
    public interface IBusinessRatingUpdater
    {
        /// <summary>
        /// Yeni bir puan eklendiğinde toplam yorum sayısını ve ortalama puanı
        /// tek bir UPDATE ifadesiyle günceller.
        /// </summary>
        /// <param name="businessId">İşletme kimliği.</param>
        /// <param name="score">Eklenen puan (1-5).</param>
        /// <param name="cancellationToken">İptal tokeni.</param>
        /// <returns>Güncellenen satır sayısı.</returns>
        Task<int> UpdateOnNewReviewAsync(Guid businessId, int score, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bir puan silindiğinde toplam yorum sayısını ve ortalama puanı
        /// tek bir UPDATE ifadesiyle günceller.
        /// </summary>
        /// <param name="businessId">İşletme kimliği.</param>
        /// <param name="score">Silinen puan (1-5).</param>
        /// <param name="cancellationToken">İptal tokeni.</param>
        /// <returns>Güncellenen satır sayısı.</returns>
        Task<int> UpdateOnReviewRemovedAsync(Guid businessId, int score, CancellationToken cancellationToken = default);

        /// <summary>
        /// Yorum güncelleme, silme ve görünürlük değişikliklerinden sonra
        /// puan ve sayacı görünür yorumlardan tek SQL ifadesiyle yeniden hesaplar.
        /// </summary>
        /// <param name="businessId">İşletme kimliği.</param>
        /// <param name="cancellationToken">İptal tokeni.</param>
        /// <returns>Güncellenen satır sayısı.</returns>
        Task<int> RecalculateAsync(Guid businessId, CancellationToken cancellationToken = default);
    }
}
