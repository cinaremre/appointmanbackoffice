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
        public static readonly Guid Biz7Id = Guid.Parse("b1000007-0000-0000-0000-000000000007");

        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.Businesses.AnyAsync())
            {
                logger.LogInformation("Businesses zaten mevcut, atlanıyor.");
                return;
            }

            var businesses = new List<Business>
            {
                CreateBusiness(Biz1Id, "Kadıköy Berberi", "Kadıköy'ün en eski berberi, 20 yıllık tecrübe.",
                    EnumBusinessType.Barber, "Caferağa Mahallesi, Moda Caddesi", "45", "3",
                    34, 438, "34710",
                    "+902161234567", "info@kadikoyberberi.com", UserSeed.Owner1Id,
                    BusinessCategorySeed.BarberId, 4.5m, 12),

                CreateBusiness(Biz2Id, "Beşiktaş Kuaför", "Kadınlara özel saç tasarım ve bakım merkezi.",
                    EnumBusinessType.Hairdresser, "Şair Nedim Caddesi", "12", "1",
                    34, 425, "34349",
                    "+902121234568", "info@besiktaskuafor.com", UserSeed.Owner2Id,
                    BusinessCategorySeed.HairdresserId, 4.8m, 25),

                CreateBusiness(Biz3Id, "Taksim Tattoo Studio", "Profesyonel dövme sanatçıları.",
                    EnumBusinessType.Tattoo, "İstiklal Caddesi", "78", "2",
                    34, 428, "34430",
                    "+902121234569", "info@taksimtattoo.com", UserSeed.Owner3Id,
                    BusinessCategorySeed.TattooId, 4.9m, 38),

                CreateBusiness(Biz4Id, "Çankaya Berber", "Ankara'nın en modern berber salonu.",
                    EnumBusinessType.Barber, "Tunalı Hilmi Caddesi", "101", "5",
                    6, 64, "06550",
                    "+903121234567", "info@cankayaberber.com", UserSeed.Owner1Id,
                    BusinessCategorySeed.BarberId, 4.2m, 8),

                CreateBusiness(Biz5Id, "Alsancak Diş Kliniği", "Modern diş sağlığı merkezi.",
                    EnumBusinessType.Dentist, "Kıbrıs Şehitleri Caddesi", "22", "4",
                    35, 476, "35220",
                    "+902321234567", "info@alsancakdis.com", UserSeed.Owner2Id,
                    BusinessCategorySeed.DentistId, 4.7m, 45),

                CreateBusiness(Biz6Id, "Nilüfer Ağda Salonu", "Hijyenik ağda ve epilasyon.",
                    EnumBusinessType.Waxing, "FSM Bulvarı", "55", "1",
                    16, 200, "16110",
                    "+902241234567", "info@niluferagda.com", UserSeed.Owner3Id,
                    BusinessCategorySeed.WaxingId, 4.3m, 6),

                // Gece vardiyası test verisi: tam gece açık işletme (00:00-24:00 mesai).
                CreateBusiness(Biz7Id, "Gece Kuaförü", "Gece boyu açık kuaför — gece vardiyası test verisi.",
                    EnumBusinessType.Hairdresser, "Gece Caddesi", "7", "1",
                    34, 438, "34710",
                    "+902121234570", "info@gecekuvaforu.com", UserSeed.Owner1Id,
                    BusinessCategorySeed.HairdresserId, 4.6m, 15)
            };

            foreach (var business in businesses)
                business.Verify("seed");

            await context.Businesses.AddRangeAsync(businesses);
            await context.SaveChangesAsync();

            logger.LogInformation("Businesses seed edildi: {Count} işletme", businesses.Count);
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

            //  KATEGORİ ATA (EKLE)
            // BusinessCategory entity'sini context'ten almak yerine ID ile set et
            typeof(Business).GetProperty("CategoryId")?.SetValue(business, categoryId);

            return business;
        }
    }
}