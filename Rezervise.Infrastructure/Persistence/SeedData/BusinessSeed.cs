using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.ValueObjects;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    public static class BusinessSeed
    {
        public static readonly Guid Biz1Id = Guid.Parse("b1000001-0000-0000-0000-000000000001");
        public static readonly Guid Biz2Id = Guid.Parse("b1000002-0000-0000-0000-000000000002");
        public static readonly Guid Biz3Id = Guid.Parse("b1000003-0000-0000-0000-000000000003");
        public static readonly Guid Biz4Id = Guid.Parse("b1000004-0000-0000-0000-000000000004");
        public static readonly Guid Biz5Id = Guid.Parse("b1000005-0000-0000-0000-000000000005");
        public static readonly Guid Biz6Id = Guid.Parse("b1000006-0000-0000-0000-000000000006");

        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.Businesses.AnyAsync())
            {
                logger.LogInformation("Businesses zaten mevcut, atlaniyor.");
                return;
            }

            var businesses = new List<Business>
            {
                CreateBusiness(Biz1Id, "Kadikoy Berberi", "Kadikoyun en eski berberi, 20 yillik tecrube.",
                    EnumBusinessType.Barber, "Caferaga Mahallesi, Moda Caddesi", "45", "3",
                    34, 438, "34710",
                    "+902161234567", "info@kadikoyberberi.com", UserSeed.Owner1Id,
                    BusinessCategorySeed.BarberId, 4.5m, 12),

                CreateBusiness(Biz2Id, "Besiktas Kuafor", "Kadinlara ozel sac tasarim ve bakim merkezi.",
                    EnumBusinessType.Hairdresser, "Sair Nedim Caddesi", "12", "1",
                    34, 425, "34349",
                    "+902121234568", "info@besiktaskuafor.com", UserSeed.Owner2Id,
                    BusinessCategorySeed.HairdresserId, 4.8m, 25),

                CreateBusiness(Biz3Id, "Taksim Tattoo Studio", "Profesyonel dovme sanatcilari.",
                    EnumBusinessType.Tattoo, "Istiklal Caddesi", "78", "2",
                    34, 428, "34430",
                    "+902121234569", "info@taksimtattoo.com", UserSeed.Owner3Id,
                    BusinessCategorySeed.TattooId, 4.9m, 38),

                CreateBusiness(Biz4Id, "Cankaya Berber", "Ankaranin en modern berber salonu.",
                    EnumBusinessType.Barber, "Tunal Hilmi Caddesi", "101", "5",
                    6, 64, "06550",
                    "+903121234567", "info@cankayaberber.com", UserSeed.Owner1Id,
                    BusinessCategorySeed.BarberId, 4.2m, 8),

                CreateBusiness(Biz5Id, "Alsancak Dis Klinigi", "Modern dis sagligi merkezi.",
                    EnumBusinessType.Dentist, "Kibris Sehitleri Caddesi", "22", "4",
                    35, 476, "35220",
                    "+902321234567", "info@alsancakdis.com", UserSeed.Owner2Id,
                    BusinessCategorySeed.DentistId, 4.7m, 45),

                CreateBusiness(Biz6Id, "Nilufer Agda Salonu", "Hijyenik agda ve epilasyon.",
                    EnumBusinessType.Waxing, "FSM Bulvari", "55", "1",
                    16, 200, "16110",
                    "+902241234567", "info@niluferagda.com", UserSeed.Owner3Id,
                    BusinessCategorySeed.WaxingId, 4.3m, 6)
            };

            foreach (var business in businesses)
                business.Verify("seed");

            await context.Businesses.AddRangeAsync(businesses);
            await context.SaveChangesAsync();

            logger.LogInformation("Businesses seed edildi: {Count} isletme", businesses.Count);
        }

        private static Business CreateBusiness(Guid id, string name, string desc, EnumBusinessType type,
            string street, string buildingNo, string aptNo, int cityId, int districtId,
            string postalCode, string phone, string email,
            Guid ownerId, Guid categoryId, decimal rating, int totalReviews)
        {
            var address = new Address(cityId, districtId, street, postalCode, buildingNo, aptNo);
            var business = new Business(id, name, type, address, new PhoneNumber(phone), new Email(email), ownerId);
            business.SetDescription(desc);
            business.UpdateRating(rating);
            for (int i = 0; i < totalReviews; i++)
                business.IncrementTotalReviews();

            //  KATEGORI ATA (EKLE)
            // BusinessCategory entity'sini context'ten almak yerine ID ile set et
            typeof(Business).GetProperty("CategoryId")?.SetValue(business, categoryId);

            return business;
        }
    }
}