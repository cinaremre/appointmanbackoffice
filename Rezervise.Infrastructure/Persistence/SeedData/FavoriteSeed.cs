using Rezervise.Domain.Entities;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    public static class FavoriteSeed
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.Favorites.AnyAsync())
            {
                logger.LogInformation("Favorites zaten mevcut, atlaniyor.");
                return;
            }

            var today = DateTime.UtcNow.Date;
            var favorites = new List<Favorite>
            {
                CreateFavorite(UserSeed.Customer1Id, BusinessSeed.Biz1Id, today.AddDays(-10), "En sevdigim berber"),
                CreateFavorite(UserSeed.Customer2Id, BusinessSeed.Biz2Id, today.AddDays(-8), null),
                CreateFavorite(UserSeed.Customer4Id, BusinessSeed.Biz3Id, today.AddDays(-3), "Dovme icin favori"),
                CreateFavorite(UserSeed.Customer1Id, BusinessSeed.Biz5Id, today.AddDays(-5), null)
            };

            await context.Favorites.AddRangeAsync(favorites);
            await context.SaveChangesAsync();

            logger.LogInformation("Favorites seed edildi: {Count} favori", favorites.Count);
        }

        private static Favorite CreateFavorite(Guid userId, Guid businessId, DateTime addedAt, string? note)
        {
            var favorite = new Favorite(Guid.NewGuid(), userId, businessId, note);
            favorite.SetAddedAtForSeed(addedAt);
            return favorite;
        }
    }
}