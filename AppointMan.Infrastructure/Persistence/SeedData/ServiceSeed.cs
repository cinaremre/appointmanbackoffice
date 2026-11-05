using AppointMan.Domain.Entities;
using AppointMan.Domain.ValueObjects;
using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppointMan.Infrastructure.Persistence.SeedData
{
    public static class ServiceSeed
    {
        public static readonly Guid Svc1Id = Guid.Parse("d1000001-0000-0000-0000-000000000001");
        public static readonly Guid Svc2Id = Guid.Parse("d1000002-0000-0000-0000-000000000002");
        public static readonly Guid Svc3Id = Guid.Parse("d1000003-0000-0000-0000-000000000003");
        public static readonly Guid Svc4Id = Guid.Parse("d1000004-0000-0000-0000-000000000004");
        public static readonly Guid Svc5Id = Guid.Parse("d1000005-0000-0000-0000-000000000005");
        public static readonly Guid Svc6Id = Guid.Parse("d1000006-0000-0000-0000-000000000006");
        public static readonly Guid Svc7Id = Guid.Parse("d1000007-0000-0000-0000-000000000007");
        public static readonly Guid Svc8Id = Guid.Parse("d1000008-0000-0000-0000-000000000008");
        public static readonly Guid Svc9Id = Guid.Parse("d1000009-0000-0000-0000-000000000009");
        public static readonly Guid Svc10Id = Guid.Parse("d1000010-0000-0000-0000-000000000010");
        public static readonly Guid Svc11Id = Guid.Parse("d1000011-0000-0000-0000-000000000011");
        public static readonly Guid Svc12Id = Guid.Parse("d1000012-0000-0000-0000-000000000012");
        public static readonly Guid Svc13Id = Guid.Parse("d1000013-0000-0000-0000-000000000013");
        public static readonly Guid Svc14Id = Guid.Parse("d1000014-0000-0000-0000-000000000014");
        public static readonly Guid Svc15Id = Guid.Parse("d1000015-0000-0000-0000-000000000015");
        public static readonly Guid Svc16Id = Guid.Parse("d1000016-0000-0000-0000-000000000016");
        public static readonly Guid Svc17Id = Guid.Parse("d1000017-0000-0000-0000-000000000017");
        public static readonly Guid Svc18Id = Guid.Parse("d1000018-0000-0000-0000-000000000018");
        public static readonly Guid Svc19Id = Guid.Parse("d1000019-0000-0000-0000-000000000019");
        public static readonly Guid Svc20Id = Guid.Parse("d1000020-0000-0000-0000-000000000020");
        public static readonly Guid Svc21Id = Guid.Parse("d1000021-0000-0000-0000-000000000021");
        public static readonly Guid Svc22Id = Guid.Parse("d1000022-0000-0000-0000-000000000022");

        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.Services.AnyAsync())
            {
                logger.LogInformation("Services zaten mevcut, atlaniyor.");
                return;
            }

            var services = new List<Service>
            {
                // Kadikoy Berberi hizmetleri
                CreateService(Svc1Id, "Sac Kesimi", "Klasik sac kesimi", 150m, 30, ServiceCategorySeed.HaircutId, BusinessSeed.Biz1Id),
                CreateService(Svc2Id, "Sakal Tirasi", "Ustura ile sakal tirasi", 100m, 20, ServiceCategorySeed.ShaveId, BusinessSeed.Biz1Id),
                CreateService(Svc3Id, "Sac Boyama", "Profesyonel sac boyama", 350m, 60, ServiceCategorySeed.ColoringId, BusinessSeed.Biz1Id),
                CreateService(Svc4Id, "Fon", "Sac sekillendirme", 80m, 15, ServiceCategorySeed.StylingId, BusinessSeed.Biz1Id),

                // Besiktas Kuafor hizmetleri
                CreateService(Svc5Id, "Kadin Sac Kesimi", "Katli ve duz kesim", 250m, 45, ServiceCategorySeed.HaircutId, BusinessSeed.Biz2Id),
                CreateService(Svc6Id, "Sac Boyama", "Balayage, ombre, rofle", 600m, 120, ServiceCategorySeed.ColoringId, BusinessSeed.Biz2Id),
                CreateService(Svc7Id, "Gelin Saci", "Gelin saci tasarimi", 1500m, 120, ServiceCategorySeed.StylingId, BusinessSeed.Biz2Id),
                CreateService(Svc8Id, "Fon", "Sac sekillendirme ve fon", 150m, 30, ServiceCategorySeed.StylingId, BusinessSeed.Biz2Id),

                // Taksim Tattoo hizmetleri
                CreateService(Svc9Id, "Kucuk Dovme", "5cm e kadar dovme", 800m, 60, ServiceCategorySeed.SmallTattooId, BusinessSeed.Biz3Id),
                CreateService(Svc10Id, "Orta Dovme", "5-15cm arasi dovme", 1500m, 120, ServiceCategorySeed.MediumTattooId, BusinessSeed.Biz3Id),
                CreateService(Svc11Id, "Buyuk Dovme", "15cm uzeri dovme", 3000m, 240, ServiceCategorySeed.LargeTattooId, BusinessSeed.Biz3Id),
                CreateService(Svc12Id, "Piercing", "Vucut piercing", 300m, 20, ServiceCategorySeed.PiercingId, BusinessSeed.Biz3Id),

                // Cankaya Berber hizmetleri
                CreateService(Svc13Id, "Sac Kesimi", "Modern sac kesimi", 180m, 30, ServiceCategorySeed.HaircutId, BusinessSeed.Biz4Id),
                CreateService(Svc14Id, "Sakal Tirasi", "Sakal sekillendirme", 120m, 20, ServiceCategorySeed.BeardTrimId, BusinessSeed.Biz4Id),

                // Alsancak Dis hizmetleri
                CreateService(Svc15Id, "Dis Temizligi", "Detartraj ve polisaj", 500m, 45, ServiceCategorySeed.DentalCleaningId, BusinessSeed.Biz5Id),
                CreateService(Svc16Id, "Dolgu", "Kompozit dolgu", 800m, 60, ServiceCategorySeed.FillingId, BusinessSeed.Biz5Id),
                CreateService(Svc17Id, "Dis Cekimi", "Basit dis cekimi", 600m, 30, ServiceCategorySeed.ExtractionId, BusinessSeed.Biz5Id),
                CreateService(Svc18Id, "Dis Beyazlatma", "Ofis tipi beyazlatma", 2500m, 90, ServiceCategorySeed.WhiteningId, BusinessSeed.Biz5Id),
                CreateService(Svc19Id, "Kanal Tedavisi", "Tek kanal", 1500m, 90, ServiceCategorySeed.RootCanalId, BusinessSeed.Biz5Id),

                // Nilufer Agda hizmetleri
                CreateService(Svc20Id, "Tum Vucut Agda", "Tum vucut agda", 400m, 90, ServiceCategorySeed.FullBodyWaxId, BusinessSeed.Biz6Id),
                CreateService(Svc21Id, "Bacak Agda", "Bacak agda", 150m, 30, ServiceCategorySeed.LegWaxId, BusinessSeed.Biz6Id),
                CreateService(Svc22Id, "Koltuk Alti Agda", "Koltuk alti", 80m, 15, ServiceCategorySeed.UnderarmWaxId, BusinessSeed.Biz6Id)
            };

            await context.Services.AddRangeAsync(services);
            await context.SaveChangesAsync();

            logger.LogInformation("Services seed edildi: {Count} hizmet", services.Count);
        }

        private static Service CreateService(Guid id, string name, string desc, decimal price,
            int duration, Guid categoryId, Guid businessId)
        {
            var service = new Service(id, name, new Money(price), duration, categoryId, businessId);
            service.SetDescription(desc);
            return service;
        }
    }
}