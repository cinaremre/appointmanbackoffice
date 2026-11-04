using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.ValueObjects;
using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace AppointMan.Infrastructure.Persistence.SeedData
{
    public static class AppDbContextSeed
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Migration'ların uygulandığından emin ol
            await context.Database.MigrateAsync();

            // 1. Kullanıcılar
            if (!await context.Users.AnyAsync())
            {
                var adminUser = new User(
                    "Admin",
                    "User",
                    new Email("admin@appointman.com"),
                    new PhoneNumber("+905551234567"),
                    EnumUserRole.SuperAdmin);

                // Şifre hash'i (gerçek hayatta BCrypt kullanılmalı)
                adminUser.SetPasswordHash("AQAAAAIAAYagAAAAEKMw7..."); // "Admin123!" hash'i

                var customerUser = new User(
                    "Ahmet",
                    "Yılmaz",
                    new Email("ahmet@example.com"),
                    new PhoneNumber("+905551234568"),
                    EnumUserRole.Customer);

                customerUser.SetPasswordHash("AQAAAAIAAYagAAAAEKMw7..."); // "Customer123!" hash'i

                await context.Users.AddRangeAsync(adminUser, customerUser);
                await context.SaveChangesAsync();
            }

            // 2. İşletme Kategorileri
            if (!await context.BusinessCategories.AnyAsync())
            {
                var categories = new[]
                {
                    new BusinessCategory("Berber", 1, EnumBusinessType.Barber),
                    new BusinessCategory("Diş Hekimi", 2, EnumBusinessType.Dentist),
                    new BusinessCategory("Dövmeci", 3, EnumBusinessType.Tattoo),
                    new BusinessCategory("Ağdacı", 4, EnumBusinessType.Waxing),
                    new BusinessCategory("Kuaför", 5, EnumBusinessType.Hairdresser),
                    new BusinessCategory("Tırnak Salonu", 6, EnumBusinessType.NailSalon),
                    new BusinessCategory("Masaj", 7, EnumBusinessType.Massage),
                    new BusinessCategory("Makyaj", 8, EnumBusinessType.Makeup),
                    new BusinessCategory("Piercing", 9, EnumBusinessType.Piercing),
                    new BusinessCategory("Diğer", 99, EnumBusinessType.Other)
                };

                await context.BusinessCategories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // 3. İşletmeler
            if (!await context.Businesses.AnyAsync())
            {
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Role == EnumUserRole.SuperAdmin);
                if (adminUser != null)
                {
                    var barberCategory = await context.BusinessCategories
                        .FirstOrDefaultAsync(c => c.CorrespondingBusinessType == EnumBusinessType.Barber);

                    var business = new Business(
                        "İstanbul Berberi",
                        EnumBusinessType.Barber,
                        new Address(
                            "İstiklal Caddesi",
                            "Beyoğlu",
                            "İstanbul",
                            "Türkiye",
                            "34430",
                            "123",
                            "5"),
                        new PhoneNumber("+905551234569"),
                        new Email("info@istanbulberberi.com"),
                        adminUser.Id);

                    business.SetCategory(barberCategory);
                    business.Verify("System");

                    await context.Businesses.AddAsync(business);
                    await context.SaveChangesAsync();

                    // 4. Çalışma Saatleri
                    var workingHours = new[]
                    {
                        new WorkingHour(DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18), business.Id),
                        new WorkingHour(DayOfWeek.Tuesday, TimeSpan.FromHours(9), TimeSpan.FromHours(18), business.Id),
                        new WorkingHour(DayOfWeek.Wednesday, TimeSpan.FromHours(9), TimeSpan.FromHours(18), business.Id),
                        new WorkingHour(DayOfWeek.Thursday, TimeSpan.FromHours(9), TimeSpan.FromHours(18), business.Id),
                        new WorkingHour(DayOfWeek.Friday, TimeSpan.FromHours(9), TimeSpan.FromHours(18), business.Id),
                        new WorkingHour(DayOfWeek.Saturday, TimeSpan.FromHours(10), TimeSpan.FromHours(16), business.Id),
                        new WorkingHour(DayOfWeek.Sunday, TimeSpan.FromHours(10), TimeSpan.FromHours(16), business.Id)
                    };
                    workingHours[6].Close(); // Pazar günü kapalı

                    await context.WorkingHours.AddRangeAsync(workingHours);

                    // 5. Hizmetler
                    var services = new[]
                    {
                        new Service("Saç Kesimi", new Money(150), 30, EnumServiceCategory.Haircut, business.Id),
                        new Service("Sakal Tıraşı", new Money(100), 20, EnumServiceCategory.Shave, business.Id),
                        new Service("Saç Boyama", new Money(300), 60, EnumServiceCategory.Coloring, business.Id),
                        new Service("Fön", new Money(80), 15, EnumServiceCategory.Styling, business.Id)
                    };

                    await context.Services.AddRangeAsync(services);
                    await context.SaveChangesAsync();
                }
            }

            // 6. Admin bildirimi
            Console.WriteLine("Seed data başarıyla eklendi!");
        }
    }
}