using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppointMan.Infrastructure.Persistence.SeedData
{
    public static class SeedOrchestrator
    {
        /// <summary>
        /// Tum seed datalarini sirayla ekler.
        /// Siralama foreign key bagimliliklarina gore yapilmistir.
        /// </summary>
        public static async Task SeedAllAsync(AppDbContext context, ILogger logger)
        {
            logger.LogInformation("=== SEED ISLEMI BASLIYOR ===");

            // Coklu instance korumasi: uygulama ayni anda birden fazla instance ile
            // acilirsa yalnizca bir tanesi seed eder. Kilit alinamazsa bu instance
            // sessizce cikar; duplicate kayit veya PK ihlali olusmaz.
            // @LockOwner='Session' kilidi baglanti kapandiginda otomatik birakir.
            await context.Database.OpenConnectionAsync();

            await using var lockCommand = context.Database.GetDbConnection().CreateCommand();
            lockCommand.CommandText =
                "DECLARE @result int; " +
                "EXEC @result = sp_getapplock @Resource = 'AppointMan.Seeding', " +
                "@LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0; " +
                "SELECT @result;";

            var lockResult = Convert.ToInt32(await lockCommand.ExecuteScalarAsync());

            if (lockResult < 0)
            {
                logger.LogInformation(
                    "Seed kilidi alinamadi (baska bir instance seed ediyor), atlaniyor.");
                return;
            }

            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // 0. Konum verileri (il -> ilce). Isletme adresleri CityId/DistrictId
                //    ile bu kayitlara bagli oldugu icin en once eklenmelidir.
                await CitySeed.SeedAsync(context, logger);
                await DistrictSeed.SeedAsync(context, logger);
                await LocalitySeed.SeedAsync(context, logger);
                await NeighborhoodSeed.SeedAsync(context, logger);

                // 1. Kategoriler (bagimsiz)
                await ServiceCategorySeed.SeedAsync(context, logger);
                await BusinessCategorySeed.SeedAsync(context, logger);

                // 2. Kullanicilar (isletme sahipleri icin gerekli)
                await UserSeed.SeedAsync(context, logger);

                // 3. Isletmeler (User + BusinessCategory gerektirir)
                await BusinessSeed.SeedAsync(context, logger);

                // 4. Isletme-hizmet kategorileri (Business + ServiceCategory gerektirir)
                await BusinessServiceCategorySeed.SeedAsync(context, logger);

                // 5. Calisanlar (Business gerektirir)
                await EmployeeSeed.SeedAsync(context, logger);

                // 6. Hizmetler (Business + ServiceCategory gerektirir)
                await ServiceSeed.SeedAsync(context, logger);

                // 7. Calisan-hizmet iliskileri (Employee + Service gerektirir)
                await EmployeeServiceSeed.SeedAsync(context, logger);

                // 8. Calisma saatleri (Business / Employee gerektirir)
                await WorkingHourSeed.SeedAsync(context, logger);
                await EmployeeWorkingHourSeed.SeedAsync(context, logger);

                // 9. Calisan izinleri (Employee gerektirir)
                await EmployeeTimeOffSeed.SeedAsync(context, logger);

                // 10. Randevular (User + Business + Service + Employee gerektirir)
                await AppointmentSeed.SeedAsync(context, logger);

                // 11. Randevu gecmisi (Appointment gerektirir)
                await AppointmentHistorySeed.SeedAsync(context, logger);

                // 12. Yorumlar (User + Business + Appointment gerektirir)
                await ReviewSeed.SeedAsync(context, logger);

                // 13. Favoriler (User + Business gerektirir)
                await FavoriteSeed.SeedAsync(context, logger);

                // 14. Bildirimler (User gerektirir)
                await NotificationSeed.SeedAsync(context, logger);

                await transaction.CommitAsync();

                logger.LogInformation("=== SEED ISLEMI TAMAMLANDI ===");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "=== SEED ISLEMI BASARISIZ, GERI ALINDI ===");
                throw;
            }
        }

        /// <summary>
        /// Tum verileri temizler (gelistirme/test ortami icin).
        /// FK kontrolleri gecici devre disi birakilarak silinir.
        /// </summary>
        public static async Task ClearAllAsync(AppDbContext context, ILogger logger)
        {
            logger.LogInformation("=== VERITABANI TEMIZLENIYOR ===");

            await context.Database.ExecuteSqlRawAsync(
                "EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");

            try
            {
                // Child -> Parent sirasi
                await context.AppointmentHistories.ExecuteDeleteAsync();
                await context.Reviews.ExecuteDeleteAsync();
                await context.Favorites.ExecuteDeleteAsync();
                await context.Notifications.ExecuteDeleteAsync();
                await context.Appointments.ExecuteDeleteAsync();
                await context.EmployeeTimeOffs.ExecuteDeleteAsync();
                await context.EmployeeWorkingHours.ExecuteDeleteAsync();
                await context.EmployeeServices.ExecuteDeleteAsync();
                await context.Employees.ExecuteDeleteAsync();
                await context.WorkingHours.ExecuteDeleteAsync();
                await context.Services.ExecuteDeleteAsync();
                await context.BusinessServiceCategories.ExecuteDeleteAsync();
                await context.Businesses.ExecuteDeleteAsync();
                await context.BusinessCategories.ExecuteDeleteAsync();
                await context.ServiceCategoryTranslations.ExecuteDeleteAsync();
                await context.ServiceCategories.ExecuteDeleteAsync();
                // Konum verileri en son silinir: isletmeler CityId/DistrictId ile
                // bu kayitlara bagli oldugu icin once alt tablolar temizlenmis olmali.
                await context.Neighborhoods.ExecuteDeleteAsync();
                await context.Localities.ExecuteDeleteAsync();
                await context.Districts.ExecuteDeleteAsync();
                await context.Cities.ExecuteDeleteAsync();
                await context.Users.ExecuteDeleteAsync();
            }
            finally
            {
                await context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL'");
            }

            logger.LogInformation("=== VERITABANI TEMIZLENDI ===");
        }
    }
}