using System;
using System.Threading;
using System.Threading.Tasks;
using Rezervise.Application.Common.Abstractions;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Rezervise.Infrastructure.Persistence.Services
{
    /// <summary>
    /// İşletme puan ve yorum sayaçlarını tek SQL UPDATE ifadesiyle günceller.
    /// Read-modify-write deseninin aksine es zamanlı işlemlerde sayaç kaybı oluşmaz.
    /// </summary>
    public sealed class BusinessRatingUpdater : IBusinessRatingUpdater
    {
        private readonly AppDbContext _context;

        public BusinessRatingUpdater(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Yeni puan eklendiğinde toplam sayacı artırır ve ortalamayı günceller.
        /// SET ifadesinin sağ tarafı SQL Server semantiği gereği ORIJINAL
        /// değerleri okur; bu yüzden ortalama eski sayaç üzerinden hesaplanır.
        /// </summary>
        public Task<int> UpdateOnNewReviewAsync(Guid businessId, int score, CancellationToken cancellationToken = default)
        {
            return _context.Businesses
                .Where(b => b.Id == businessId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(b => b.TotalReviews, b => b.TotalReviews + 1)
                        .SetProperty(
                            b => b.Rating,
                            b => ((b.Rating ?? 0m) * b.TotalReviews + score) / (b.TotalReviews + 1)),
                    cancellationToken);
        }

        /// <summary>
        /// Puan silindiğinde toplam sayacı azaltır ve ortalamayı günceller.
        /// Sayaç 1 veya daha azsa ortalama sıfırlanır.
        /// </summary>
        public Task<int> UpdateOnReviewRemovedAsync(Guid businessId, int score, CancellationToken cancellationToken = default)
        {
            return _context.Businesses
                .Where(b => b.Id == businessId && b.TotalReviews > 0)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(b => b.TotalReviews, b => b.TotalReviews - 1)
                        .SetProperty(
                            b => b.Rating,
                            b => b.TotalReviews <= 1
                                ? 0m
                                : ((b.Rating ?? 0m) * b.TotalReviews - score) / (b.TotalReviews - 1)),
                    cancellationToken);
        }

        /// <summary>
        /// Görünür yorumlardan puan ve sayacı yeniden hesaplar. Yorum güncelleme,
        /// silme ve görünürlük değişikliklerinde artımlı aritmetiğe güvenmek yerine
        /// tek bir UPDATE ile kesin değerler yazılır; böylece TotalReviews kaymaz.
        /// </summary>
        public async Task<int> RecalculateAsync(Guid businessId, CancellationToken cancellationToken = default)
        {
            // Önce yorum değişikliğinin SaveChanges ile veritabanına yazıldığından
            // emin olunmalı; aksi halde silinen/gizlenen yorum hâlâ sayılır.
            var stats = await _context.Reviews
                .Where(r => r.BusinessId == businessId && r.IsVisible && !r.IsDeleted)
                .GroupBy(r => r.BusinessId)
                .Select(g => new
                {
                    Count = g.Count(),
                    Average = g.Average(r => (decimal)r.Rating)
                })
                .FirstOrDefaultAsync(cancellationToken);

            return await _context.Businesses
                .Where(b => b.Id == businessId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(b => b.TotalReviews, stats?.Count ?? 0)
                        .SetProperty(b => b.Rating, Math.Round(stats?.Average ?? 0m, 1)),
                    cancellationToken);
        }
    }
}
