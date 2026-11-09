using AppointMan.Domain.Entities;
using AppointMan.Domain.ValueObjects;
using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppointMan.Infrastructure.Persistence.SeedData
{
    public static class EmployeeSeed
    {
        public static readonly Guid Emp1Id = Guid.Parse("c1000001-0000-0000-0000-000000000001");
        public static readonly Guid Emp2Id = Guid.Parse("c1000002-0000-0000-0000-000000000002");
        public static readonly Guid Emp3Id = Guid.Parse("c1000003-0000-0000-0000-000000000003");
        public static readonly Guid Emp4Id = Guid.Parse("c1000004-0000-0000-0000-000000000004");
        public static readonly Guid Emp5Id = Guid.Parse("c1000005-0000-0000-0000-000000000005");
        public static readonly Guid Emp6Id = Guid.Parse("c1000006-0000-0000-0000-000000000006");
        public static readonly Guid Emp7Id = Guid.Parse("c1000007-0000-0000-0000-000000000007");
        public static readonly Guid Emp8Id = Guid.Parse("c1000008-0000-0000-0000-000000000008");
        public static readonly Guid Emp9Id = Guid.Parse("c1000009-0000-0000-0000-000000000009");
        public static readonly Guid Emp10Id = Guid.Parse("c1000010-0000-0000-0000-000000000010");
        public static readonly Guid Emp11Id = Guid.Parse("c1000011-0000-0000-0000-000000000011");
        public static readonly Guid Emp12Id = Guid.Parse("c1000012-0000-0000-0000-000000000012");

        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.Employees.AnyAsync())
            {
                logger.LogInformation("Employees zaten mevcut, atlaniyor.");
                return;
            }

            var employees = new List<Employee>
            {
                CreateEmployee(Emp1Id, "Hasan", "Usta", "Usta Berber", "+905551110001",
                    BusinessSeed.Biz1Id, 1),

                CreateEmployee(Emp2Id, "Emre", "Genc", "Berber", "+905551110002",
                    BusinessSeed.Biz1Id, 2),

                CreateEmployee(Emp3Id, "Murat", "Kidemli", "Kidemli Berber", "+905551110003",
                    BusinessSeed.Biz1Id, 3),

                CreateEmployee(Emp4Id, "Selin", "Sac", "Bas Kuafor", "+905551110004",
                    BusinessSeed.Biz2Id, 1),

                CreateEmployee(Emp5Id, "Deniz", "Stil", "Kuafor", "+905551110005",
                    BusinessSeed.Biz2Id, 2),

                CreateEmployee(Emp6Id, "Kaan", "Dovmeci", "Dovme Sanatcisi", "+905551110006",
                    BusinessSeed.Biz3Id, 1),

                CreateEmployee(Emp7Id, "Buse", "Sanat", "Dovme Sanatcisi", "+905551110007",
                    BusinessSeed.Biz3Id, 2),

                CreateEmployee(Emp8Id, "Onur", "Cizim", "Piercing Uzmani", "+905551110008",
                    BusinessSeed.Biz3Id, 3),

                CreateEmployee(Emp9Id, "Ibrahim", "Usta", "Usta Berber", "+905551110009",
                    BusinessSeed.Biz4Id, 1),

                CreateEmployee(Emp10Id, "Aylin", "Dis", "Dis Hekimi", "+905551110010",
                    BusinessSeed.Biz5Id, 1),

                CreateEmployee(Emp11Id, "Cem", "Saglik", "Dis Hekimi", "+905551110011",
                    BusinessSeed.Biz5Id, 2),

                CreateEmployee(Emp12Id, "Fatma", "Uzman", "Agda Uzmani", "+905551110012",
                    BusinessSeed.Biz6Id, 1)
            };

            // Onur (Emp8) randevu kabul etmiyor
            var onur = employees.First(e => e.Id == Emp8Id);
            onur.MakeUnavailableForBooking();

            await context.Employees.AddRangeAsync(employees);
            await context.SaveChangesAsync();

            // Calisanlari kullanicilara bagla
            var hasan = employees.First(e => e.Id == Emp1Id);
            var selin = employees.First(e => e.Id == Emp4Id);
            var kaan = employees.First(e => e.Id == Emp6Id);

            var hasanUser = await context.Users.FirstAsync(u => u.Email == "hasan.usta@example.com");
            var selinUser = await context.Users.FirstAsync(u => u.Email == "selin.sac@example.com");
            var kaanUser = await context.Users.FirstAsync(u => u.Email == "kaan.dovmeci@example.com");

            hasan.LinkToUser(hasanUser);
            selin.LinkToUser(selinUser);
            kaan.LinkToUser(kaanUser);

            await context.SaveChangesAsync();

            logger.LogInformation("Employees seed edildi: {Count} calisan (3 tanesi kullaniciya bagli)", employees.Count);
        }

        private static Employee CreateEmployee(Guid id, string firstName, string lastName,
            string title, string phone, Guid businessId, int displayOrder)
        {
            var employee = new Employee(id, firstName, lastName, new PhoneNumber(phone), businessId);
            employee.SetTitle(title);
            employee.SetDisplayOrder(displayOrder);
            return employee;
        }
    }
}