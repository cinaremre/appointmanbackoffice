using Rezervise.Domain.Entities;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    public static class BusinessServiceCategorySeed
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.BusinessServiceCategories.AnyAsync())
            {
                logger.LogInformation("BusinessServiceCategories zaten mevcut, atlanıyor.");
                return;
            }

            // Her işletme için root hizmet kategorilerini ata
            var assignments = new List<(Guid BusinessId, Guid ServiceCategoryId)>
            {
                // Kadıkoy Berberi: Saç + Sakal
                (BusinessSeed.Biz1Id, ServiceCategorySeed.HairServicesId),
                (BusinessSeed.Biz1Id, ServiceCategorySeed.BeardShaveId),

                // Beşiktaş Kuaför: Saç + Makyaj + Tırnak
                (BusinessSeed.Biz2Id, ServiceCategorySeed.HairServicesId),
                (BusinessSeed.Biz2Id, ServiceCategorySeed.MakeupId),
                (BusinessSeed.Biz2Id, ServiceCategorySeed.NailCareId),

                // Taksim Tattoo: Dövme
                (BusinessSeed.Biz3Id, ServiceCategorySeed.TattooPiercingId),

                // Çankaya Berber: Saç + Sakal
                (BusinessSeed.Biz4Id, ServiceCategorySeed.HairServicesId),
                (BusinessSeed.Biz4Id, ServiceCategorySeed.BeardShaveId),

                // Alsancak Dış: Dış
                (BusinessSeed.Biz5Id, ServiceCategorySeed.DentalServicesId),

                // Nilüfer Ağda: Epilasyon
                (BusinessSeed.Biz6Id, ServiceCategorySeed.HairRemovalId),

                // Gece Kuaförü: Saç (gece vardiyası test verisi)
                (BusinessSeed.Biz7Id, ServiceCategorySeed.HairServicesId)
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