using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    public static class SeedOrchestrator
    {
        /// <summary>
        /// Tüm seed datalarını sırayla ekler.
        /// Sıralama foreign key bağımlılıklarına göre yapılmıştır.
        /// </summary>
        public static async Task SeedAllAsync(AppDbContext context, ILogger logger)
        {
            logger.LogInformation("=== SEED İŞLEMİ BAŞLIYOR ===");

            // Çoklu instance koruması: uygulama aynı anda birden fazla instance ile
            // açılırsa yalnızca bir tanesi seed eder. Kilit alınamazsa bu instance
            // sessizce çıkar; duplicate kayıt veya PK ihlali oluşmaz.
            // @LockOwner='Session' kilidi bağlantı kapandığında otomatik bırakır.
            await context.Database.OpenConnectionAsync();

            await using var lockCommand = context.Database.GetDbConnection().CreateCommand();
            lockCommand.CommandText =
                "DECLARE @result int; " +
                "EXEC @result = sp_getapplock @Resource = 'Rezervise.Seeding', " +
                "@LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0; " +
                "SELECT @result;";

            var lockResult = Convert.ToInt32(await lockCommand.ExecuteScalarAsync());

            if (lockResult < 0)
            {
                logger.LogInformation(
                    "Seed kilidi alınamadı (başka bir instance seed ediyor), atlanıyor.");
                return;
            }

            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // 0. Konum verileri (il -> ilçe). İşletme adresleri CityId/DistrictId
                //    ile bu kayıtlara bağlı olduğu için en önce eklenmelidir.
                await CitySeed.SeedAsync(context, logger);
                await DistrictSeed.SeedAsync(context, logger);
                await LocalitySeed.SeedAsync(context, logger);
                await NeighborhoodSeed.SeedAsync(context, logger);

                // 1. Kategoriler (bağımsız)
                await ServiceCategorySeed.SeedAsync(context, logger);
                await BusinessCategorySeed.SeedAsync(context, logger);

                // 2. Kullanıcılar (işletme sahipleri için gerekli)
                await UserSeed.SeedAsync(context, logger);

                // 3. İşletmeler (User + BusinessCategory gerektirir)
                await BusinessSeed.SeedAsync(context, logger);

                // 4. İşletme-hizmet kategorileri (Business + ServiceCategory gerektirir)
                await BusinessServiceCategorySeed.SeedAsync(context, logger);

                // 5. Çalışanlar (Business gerektirir)
                await EmployeeSeed.SeedAsync(context, logger);

                // 6. Hizmetler (Business + ServiceCategory gerektirir)
                await ServiceSeed.SeedAsync(context, logger);

                // 7. Çalışan-hizmet ilişkileri (Employee + Service gerektirir)
                await EmployeeServiceSeed.SeedAsync(context, logger);

                // 8. Çalışma saatleri (Business / Employee gerektirir)
                await WorkingHourSeed.SeedAsync(context, logger);
                await EmployeeWorkingHourSeed.SeedAsync(context, logger);

                // 9. Çalışan izinleri (Employee gerektirir)
                await EmployeeTimeOffSeed.SeedAsync(context, logger);

                // 10. Randevular (User + Business + Service + Employee gerektirir)
                await AppointmentSeed.SeedAsync(context, logger);

                // 11. Randevu geçmişi (Appointment gerektirir)
                await AppointmentHistorySeed.SeedAsync(context, logger);

                // 12. Yorumlar (User + Business + Appointment gerektirir)
                await ReviewSeed.SeedAsync(context, logger);

                // 13. Favoriler (User + Business gerektirir)
                await FavoriteSeed.SeedAsync(context, logger);

                // 14. Bildirimler (User gerektirir)
                await NotificationSeed.SeedAsync(context, logger);

                await transaction.CommitAsync();

                logger.LogInformation("=== SEED İŞLEMİ TAMAMLANDI ===");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "=== SEED İŞLEMİ BAŞARISIZ, GERİ ALINDI ===");
                throw;
            }
        }

        /// <summary>
        /// Tüm verileri temizler (geliştirme/test ortamı için).
        /// FK kontrolleri geçici devre dışı bırakılarak silinir.
        /// </summary>
        public static async Task ClearAllAsync(AppDbContext context, ILogger logger)
        {
            logger.LogInformation("=== VERİTABANI TEMİZLENİYOR ===");

            await context.Database.ExecuteSqlRawAsync(
                "EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");

            try
            {
                // Child -> Parent sırası
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
                // Konum verileri en son silinir: işletmeler CityId/DistrictId ile
                // bu kayıtlara bağlı olduğu için önce alt tablolar temizlenmiş olmalı.
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

            logger.LogInformation("=== VERİTABANI TEMİZLENDİ ===");
        }
    }
}