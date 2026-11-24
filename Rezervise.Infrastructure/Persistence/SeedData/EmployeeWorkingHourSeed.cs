using Rezervise.Domain.Entities;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    public static class EmployeeWorkingHourSeed
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.EmployeeWorkingHours.AnyAsync())
            {
                logger.LogInformation("EmployeeWorkingHours zaten mevcut, atlanıyor.");
                return;
            }

            var workingHours = new List<EmployeeWorkingHour>();

            // Hasan Usta (Emp1): Pzt-Cmt, 12-13 mola
            AddSchedule(workingHours, EmployeeSeed.Emp1Id, 6,
                TimeSpan.FromHours(9), TimeSpan.FromHours(18),
                TimeSpan.FromHours(12), TimeSpan.FromHours(13));

            // Emre Genc (Emp2): Salı-Cmt
            for (int day = 2; day <= 6; day++)
            {
                var wh = new EmployeeWorkingHour(Guid.NewGuid(), EmployeeSeed.Emp2Id, (DayOfWeek)day,
                    TimeSpan.FromHours(10), TimeSpan.FromHours(19));
                wh.SetBreakTime(TimeSpan.FromHours(13), TimeSpan.FromHours(14));
                workingHours.Add(wh);
            }

            // Murat Kıdemli (Emp3): Pzt-Cmt, 13-14 mola
            AddSchedule(workingHours, EmployeeSeed.Emp3Id, 6,
                TimeSpan.FromHours(9), TimeSpan.FromHours(18),
                TimeSpan.FromHours(13), TimeSpan.FromHours(14));

            // Selin Saç (Emp4): Pzt-Cmt, 12:30-13:30 mola
            AddSchedule(workingHours, EmployeeSeed.Emp4Id, 6,
                TimeSpan.FromHours(9), TimeSpan.FromHours(19),
                TimeSpan.FromHours(12.5), TimeSpan.FromHours(13.5));

            // Deniz Stil (Emp5): Salı-Cmt
            for (int day = 2; day <= 6; day++)
            {
                var wh = new EmployeeWorkingHour(Guid.NewGuid(), EmployeeSeed.Emp5Id, (DayOfWeek)day,
                    TimeSpan.FromHours(10), TimeSpan.FromHours(18));
                wh.SetBreakTime(TimeSpan.FromHours(13), TimeSpan.FromHours(14));
                workingHours.Add(wh);
            }

            // Kaan Dövmeci (Emp6): Pzt-Cmt, 14-15 mola
            AddSchedule(workingHours, EmployeeSeed.Emp6Id, 6,
                TimeSpan.FromHours(11), TimeSpan.FromHours(20),
                TimeSpan.FromHours(14), TimeSpan.FromHours(15));

            // Buse Sanat (Emp7): Salı-Cmt
            for (int day = 2; day <= 6; day++)
            {
                var wh = new EmployeeWorkingHour(Guid.NewGuid(), EmployeeSeed.Emp7Id, (DayOfWeek)day,
                    TimeSpan.FromHours(12), TimeSpan.FromHours(19));
                workingHours.Add(wh);
            }

            // İbrahim Usta (Emp9): Pzt-Cmt, 13-14 mola
            AddSchedule(workingHours, EmployeeSeed.Emp9Id, 6,
                TimeSpan.FromHours(8), TimeSpan.FromHours(20),
                TimeSpan.FromHours(13), TimeSpan.FromHours(14));

            // Dr. Aylın (Emp10): Pzt-Cmt, 12-13 mola
            AddSchedule(workingHours, EmployeeSeed.Emp10Id, 6,
                TimeSpan.FromHours(9), TimeSpan.FromHours(18),
                TimeSpan.FromHours(12), TimeSpan.FromHours(13));

            // Dr. Cem (Emp11): Salı-Cmt
            for (int day = 2; day <= 6; day++)
            {
                var wh = new EmployeeWorkingHour(Guid.NewGuid(), EmployeeSeed.Emp11Id, (DayOfWeek)day,
                    TimeSpan.FromHours(9), TimeSpan.FromHours(17));
                wh.SetBreakTime(TimeSpan.FromHours(12), TimeSpan.FromHours(13));
                workingHours.Add(wh);
            }

            // Fatma Uzman (Emp12): Pzt-Cmt, 13-14 mola
            AddSchedule(workingHours, EmployeeSeed.Emp12Id, 6,
                TimeSpan.FromHours(10), TimeSpan.FromHours(19),
                TimeSpan.FromHours(13), TimeSpan.FromHours(14));

            // Nur Gece (Emp13): gece vardiyası — her gün 00:00-08:00 (yerel gece yarısı dahil).
            // NOT: EmployeeWorkingHour tek kayıtla gece yarısını geçemez (start < end kuralı);
            // "akşam 20:00'den sabaha" kesintisiz vardiya bu nedenle 00:00-08:00 olarak seed edilir.
            for (int day = 0; day <= 6; day++)
            {
                workingHours.Add(new EmployeeWorkingHour(Guid.NewGuid(), EmployeeSeed.Emp13Id, (DayOfWeek)day,
                    TimeSpan.FromHours(0), TimeSpan.FromHours(8)));
            }

            await context.EmployeeWorkingHours.AddRangeAsync(workingHours);
            await context.SaveChangesAsync();

            logger.LogInformation("EmployeeWorkingHours seed edildi: {Count} kayıt", workingHours.Count);
        }

        private static void AddSchedule(List<EmployeeWorkingHour> list, Guid employeeId,
            int dayCount, TimeSpan start, TimeSpan end, TimeSpan? breakStart = null, TimeSpan? breakEnd = null)
        {
            for (int day = 1; day <= dayCount; day++)
            {
                var wh = new EmployeeWorkingHour(Guid.NewGuid(), employeeId, (DayOfWeek)day, start, end);
                if (breakStart.HasValue && breakEnd.HasValue)
                    wh.SetBreakTime(breakStart, breakEnd);
                list.Add(wh);
            }
        }
    }
}