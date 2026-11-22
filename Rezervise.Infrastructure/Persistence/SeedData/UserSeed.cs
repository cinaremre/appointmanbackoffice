using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.ValueObjects;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    public static class UserSeed
    {
        // Sabit GUID'ler - diger seed dosyalarinda referans icin
        public static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid Owner1Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid Owner2Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid Owner3Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid Customer1Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid Customer2Id = Guid.Parse("66666666-6666-6666-6666-666666666666");
        public static readonly Guid Customer3Id = Guid.Parse("77777777-7777-7777-7777-777777777777");
        public static readonly Guid Customer4Id = Guid.Parse("88888888-8888-8888-8888-888888888888");
        public static readonly Guid Customer5Id = Guid.Parse("99999999-9999-9999-9999-999999999999");

        // Calisan kullanicilar
        public static readonly Guid EmployeeUser1Id = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000001");
        public static readonly Guid EmployeeUser2Id = Guid.Parse("aaaaaaaa-0002-0000-0000-000000000002");
        public static readonly Guid EmployeeUser3Id = Guid.Parse("aaaaaaaa-0003-0000-0000-000000000003");

        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.Users.AnyAsync())
            {
                logger.LogInformation("Users zaten mevcut, atlaniyor.");
                return;
            }

            var users = new List<User>
            {
                CreateUser(AdminId, "Sistem", "Admin", "admin@example.com", "+905550000001",
                    EnumUserRole.SuperAdmin, "1985-05-15", true, true),

                CreateUser(Owner1Id, "Mehmet", "Yilmaz", "businessowner1@example.com", "+905550000002",
                    EnumUserRole.BusinessOwner, "1988-03-20", true, true),

                CreateUser(Owner2Id, "Ayse", "Kaya", "businessowner2@example.com", "+905550000003",
                    EnumUserRole.BusinessOwner, "1990-07-12", true, true),

                CreateUser(Owner3Id, "Ali", "Demir", "businessowner3@example.com", "+905550000004",
                    EnumUserRole.BusinessOwner, "1992-11-08", true, false),

                CreateUser(Customer1Id, "Ahmet", "Celik", "customer1@example.com", "+905550000005",
                    EnumUserRole.Customer, "1995-01-25", true, true),

                CreateUser(Customer2Id, "Zeynep", "Arslan", "customer2@example.com", "+905550000006",
                    EnumUserRole.Customer, "1997-06-18", true, false),

                CreateUser(Customer3Id, "Can", "Ozturk", "customer3@example.com", "+905550000007",
                    EnumUserRole.Customer, "1993-09-30", false, false),

                CreateUser(Customer4Id, "Elif", "Sahin", "customer4@example.com", "+905550000008",
                    EnumUserRole.Customer, "1999-12-05", true, true),

                CreateUser(Customer5Id, "Burak", "Yildiz", "customer5@example.com", "+905550000009",
                    EnumUserRole.Customer, "1991-04-22", true, false),

                // Calisan kullanicilar
                CreateUser(EmployeeUser1Id, "Hasan", "Usta", "hasan.usta@example.com", "+905550000010",
                    EnumUserRole.Customer, "1980-01-01", true, true),

                CreateUser(EmployeeUser2Id, "Selin", "Sac", "selin.sac@example.com", "+905550000011",
                    EnumUserRole.Customer, "1985-06-15", true, true),

                CreateUser(EmployeeUser3Id, "Kaan", "Dovmeci", "kaan.dovmeci@example.com", "+905550000012",
                    EnumUserRole.Customer, "1990-03-20", true, true)
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            logger.LogInformation("Users seed edildi: {Count} kullanici", users.Count);
        }

        private static User CreateUser(Guid id, string firstName, string lastName, string email,
            string phone, EnumUserRole role, string? dateOfBirth, bool emailVerified, bool phoneVerified)
        {

            const string passwordHash = "$2a$11$B67U1BFFsEe3KXoHu55b4ezNS5ItlHvYuddAqQz4Tid3WretjONHq";
            var user = new User(id, firstName, lastName, new Email(email), new PhoneNumber(phone), role);

            user.SetPasswordHash(passwordHash);
            if (dateOfBirth != null)
                user.SetDateOfBirth(DateTime.Parse(dateOfBirth));
            if (emailVerified) user.VerifyEmail();
            if (phoneVerified) user.VerifyPhone();
            return user;
        }
    }
}