using Rezervise.Domain.Entities;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    public static class ReviewSeed
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.Reviews.AnyAsync())
            {
                logger.LogInformation("Reviews zaten mevcut, atlanıyor.");
                return;
            }

            var today = DateTime.UtcNow.Date;
            var reviews = new List<Review>
            {
                CreateReview(UserSeed.Customer1Id, BusinessSeed.Biz1Id, AppointmentSeed.Apt1Id,
                    5, "Hasan Usta harika bir kesim yaptı, kesinlikle tavsiye ederim.",
                    today.AddDays(-9), true),

                CreateReview(UserSeed.Customer2Id, BusinessSeed.Biz2Id, AppointmentSeed.Apt2Id,
                    4, "Selin Hanım çok ilgiliydi, saçım güzel oldu. Biraz bekledim.",
                    today.AddDays(-7), true),

                CreateReview(UserSeed.Customer4Id, BusinessSeed.Biz3Id, AppointmentSeed.Apt4Id,
                    5, "Kaan çok yetenekli bir sanatçı, dövmem tam istediğim gibi oldu.",
                    today.AddDays(-2), true),

                CreateReview(UserSeed.Customer5Id, BusinessSeed.Biz4Id, AppointmentSeed.Apt5Id,
                    3, "Fena değildi ama çok bekledim. Kesim idare eder.",
                    today.AddDays(-1), true),

                CreateReview(UserSeed.Customer1Id, BusinessSeed.Biz2Id, null,
                    2, "Saçım istediğim gibi olmadı, memnun kalmadım.",
                    today.AddDays(-20), false)  // gizli
            };

            await context.Reviews.AddRangeAsync(reviews);
            await context.SaveChangesAsync();

            logger.LogInformation("Reviews seed edildi: {Count} yorum", reviews.Count);
        }

        private static Review CreateReview(Guid userId, Guid businessId, Guid? appointmentId,
            int rating, string comment, DateTime reviewDate, bool isVisible)
        {
            var review = new Review(Guid.NewGuid(), userId, businessId, rating, comment, appointmentId);
            review.SetReviewDateForSeed(reviewDate);
            if (!isVisible)
                review.Hide();
            return review;
        }
    }
}