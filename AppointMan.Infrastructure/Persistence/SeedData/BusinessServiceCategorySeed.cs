using AppointMan.Domain.Entities;
using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppointMan.Infrastructure.Persistence.SeedData
{
    public static class BusinessServiceCategorySeed
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.BusinessServiceCategories.AnyAsync())
            {
                logger.LogInformation("BusinessServiceCategories zaten mevcut, atlaniyor.");
                return;
            }

            // Her isletme icin root hizmet kategorilerini ata
            var assignments = new List<(Guid BusinessId, Guid ServiceCategoryId)>
            {
                // Kadikoy Berberi: Sac + Sakal
                (BusinessSeed.Biz1Id, ServiceCategorySeed.HairServicesId),
                (BusinessSeed.Biz1Id, ServiceCategorySeed.BeardShaveId),

                // Besiktas Kuafor: Sac + Makyaj + Tirnak
                (BusinessSeed.Biz2Id, ServiceCategorySeed.HairServicesId),
                (BusinessSeed.Biz2Id, ServiceCategorySeed.MakeupId),
                (BusinessSeed.Biz2Id, ServiceCategorySeed.NailCareId),

                // Taksim Tattoo: Dovme
                (BusinessSeed.Biz3Id, ServiceCategorySeed.TattooPiercingId),

                // Cankaya Berber: Sac + Sakal
                (BusinessSeed.Biz4Id, ServiceCategorySeed.HairServicesId),
                (BusinessSeed.Biz4Id, ServiceCategorySeed.BeardShaveId),

                // Alsancak Dis: Dis
                (BusinessSeed.Biz5Id, ServiceCategorySeed.DentalServicesId),

                // Nilufer Agda: Epilasyon
                (BusinessSeed.Biz6Id, ServiceCategorySeed.HairRemovalId)
            };

            var businessServiceCategories = assignments
                .Select(a => new BusinessServiceCategory(Guid.NewGuid(), a.BusinessId, a.ServiceCategoryId))
                .ToList();

            await context.BusinessServiceCategories.AddRangeAsync(businessServiceCategories);
            await context.SaveChangesAsync();

            logger.LogInformation("BusinessServiceCategories seed edildi: {Count} atama", businessServiceCategories.Count);
        }
    }
}