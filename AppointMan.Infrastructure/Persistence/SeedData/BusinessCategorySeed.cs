using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppointMan.Infrastructure.Persistence.SeedData
{
    public static class BusinessCategorySeed
    {
        // Sabit GUID'ler
        public static readonly Guid BarberId = Guid.Parse("a1000001-0000-0000-0000-000000000001");
        public static readonly Guid DentistId = Guid.Parse("a1000002-0000-0000-0000-000000000002");
        public static readonly Guid TattooId = Guid.Parse("a1000003-0000-0000-0000-000000000003");
        public static readonly Guid WaxingId = Guid.Parse("a1000004-0000-0000-0000-000000000004");
        public static readonly Guid HairdresserId = Guid.Parse("a1000005-0000-0000-0000-000000000005");
        public static readonly Guid NailSalonId = Guid.Parse("a1000006-0000-0000-0000-000000000006");
        public static readonly Guid SpaId = Guid.Parse("a1000007-0000-0000-0000-000000000007");
        public static readonly Guid MassageId = Guid.Parse("a1000008-0000-0000-0000-000000000008");

        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.BusinessCategories.AnyAsync())
            {
                logger.LogInformation("BusinessCategories zaten mevcut, atlaniyor.");
                return;
            }

            var categories = new List<BusinessCategory>
            {
                CreateCategory(BarberId, "Berber", "Erkek kuaforu ve berber hizmetleri", 1, EnumBusinessType.Barber),
                CreateCategory(DentistId, "Dis Hekimi", "Dis sagligi ve tedavi hizmetleri", 2, EnumBusinessType.Dentist),
                CreateCategory(TattooId, "Dovmeci", "Dovme ve vucut sanati", 3, EnumBusinessType.Tattoo),
                CreateCategory(WaxingId, "Agdaci", "Epilasyon ve agda hizmetleri", 4, EnumBusinessType.Waxing),
                CreateCategory(HairdresserId, "Kuafor", "Kadin kuaforu ve sac hizmetleri", 5, EnumBusinessType.Hairdresser),
                CreateCategory(NailSalonId, "Tirnak Salonu", "Manikur, pedikur ve tirnak sanati", 6, EnumBusinessType.NailSalon),
                CreateCategory(SpaId, "Spa", "Wellness ve guzellik merkezi", 7, EnumBusinessType.Spa),
                CreateCategory(MassageId, "Masaj", "Terapotik masaj hizmetleri", 8, EnumBusinessType.Massage)
            };

            await context.BusinessCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();

            logger.LogInformation("BusinessCategories seed edildi: {Count} kategori", categories.Count);
        }

        private static BusinessCategory CreateCategory(Guid id, string name, string desc, int order, EnumBusinessType type)
        {
            var category = new BusinessCategory(id, name, order, type);
            category.SetDescription(desc);
            return category;
        }
    }
}