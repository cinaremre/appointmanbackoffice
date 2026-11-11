using System;
using System.Threading;
using System.Threading.Tasks;
using AppointMan.Application.Common.Abstractions;
using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AppointMan.Infrastructure.Persistence.Services
{
    /// <summary>
    /// Isletme puan ve yorum sayaclarini tek SQL UPDATE ifadesiyle gunceller.
    /// Read-modify-write deseninin aksine es zamanli islemlerde sayac kaybi olusmaz.
    /// </summary>
    public sealed class BusinessRatingUpdater : IBusinessRatingUpdater
    {
        private readonly AppDbContext _context;

        public BusinessRatingUpdater(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Yeni puan eklendiginde toplam sayaci artirir ve ortalamayi gunceller.
        /// SET ifadesinin sag tarafi SQL Server semantigi geregi ORIJINAL
        /// degerleri okur; bu yuzden ortalama eski sayac uzerinden hesaplanir.
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
        /// Puan silindiginde toplam sayaci azaltir ve ortalamayi gunceller.
        /// Sayac 1 veya daha azsa ortalama sifirlanir.
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
    }
}
