using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    public static class EmployeeTimeOffSeed
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.EmployeeTimeOffs.AnyAsync())
            {
                logger.LogInformation("EmployeeTimeOffs zaten mevcut, atlanıyor.");
                return;
            }

            var today = DateTime.UtcNow.Date;

            var timeOffs = new List<EmployeeTimeOff>
            {
                // Emre Genc: 3 günlük yıllık izin (gelecek)
                CreateTimeOff(EmployeeSeed.Emp2Id, today.AddDays(3), today.AddDays(5),
                    EnumTimeOffType.Vacation, "Yıllık izin", true, UserSeed.Owner1Id.ToString()),

                // Buse Sanat: 1 günlük hastalık izni (yarın)
                CreateTimeOff(EmployeeSeed.Emp7Id, today.AddDays(1), today.AddDays(2),
                    EnumTimeOffType.SickLeave, "Grip", true, UserSeed.Owner3Id.ToString()),

                // Dr. Cem: 5 günlük tatil (10 gün sonra)
                CreateTimeOff(EmployeeSeed.Emp11Id, today.AddDays(10), today.AddDays(14),
                    EnumTimeOffType.Vacation, "Tatil", true, UserSeed.Owner2Id.ToString())
            };

            await context.EmployeeTimeOffs.AddRangeAsync(timeOffs);
            await context.SaveChangesAsync();

            logger.LogInformation("EmployeeTimeOffs seed edildi: {Count} kayıt", timeOffs.Count);
        }

        private static EmployeeTimeOff CreateTimeOff(Guid employeeId, DateTime start, DateTime end,
            EnumTimeOffType type, string reason, bool approved, string approvedBy)
        {
            var timeOff = new EmployeeTimeOff(Guid.NewGuid(), employeeId, start, end, type, reason);
            if (approved)
                timeOff.Approve(approvedBy);
            return timeOff;
        }
    }
}