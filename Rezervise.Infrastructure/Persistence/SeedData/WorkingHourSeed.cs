using Rezervise.Domain.Entities;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    public static class WorkingHourSeed
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.WorkingHours.AnyAsync())
            {
                logger.LogInformation("WorkingHours zaten mevcut, atlanıyor.");
                return;
            }

            var workingHours = new List<WorkingHour>();

            // Kadıkoy Berberi: Pzt-Cmt 09-18, Pazar kapalı
            AddWeekSchedule(workingHours, BusinessSeed.Biz1Id,
                TimeSpan.FromHours(9), TimeSpan.FromHours(18), TimeSpan.FromHours(10), TimeSpan.FromHours(16));

            // Beşiktaş Kuaför: Pzt-Cmt 09-19, Pazar kapalı
            AddWeekSchedule(workingHours, BusinessSeed.Biz2Id,
                TimeSpan.FromHours(9), TimeSpan.FromHours(19), TimeSpan.FromHours(9), TimeSpan.FromHours(17));

            // Taksim Tattoo: Pzt-Cmt 11-20, Pazar kapalı
            AddWeekSchedule(workingHours, BusinessSeed.Biz3Id,
                TimeSpan.FromHours(11), TimeSpan.FromHours(20), TimeSpan.FromHours(12), TimeSpan.FromHours(18));

            // Çankaya Berber: Her gün 08-20 (Pazar 10-15)
            AddWeekSchedule(workingHours, BusinessSeed.Biz4Id,
                TimeSpan.FromHours(8), TimeSpan.FromHours(20), TimeSpan.FromHours(10), TimeSpan.FromHours(15), sundayOpen: true);

            // Alsancak Dış: Pzt-Cmt 09-18, Pazar kapalı
            AddWeekSchedule(workingHours, BusinessSeed.Biz5Id,
                TimeSpan.FromHours(9), TimeSpan.FromHours(18), TimeSpan.FromHours(10), TimeSpan.FromHours(15));

            // Nilüfer Ağda: Pzt-Cmt 10-19, Pazar kapalı
            AddWeekSchedule(workingHours, BusinessSeed.Biz6Id,
                TimeSpan.FromHours(10), TimeSpan.FromHours(19), TimeSpan.FromHours(10), TimeSpan.FromHours(17));

            // Gece Kuaförü: her gün 00:00-24:00 (tam gece dahil açık)
            AddWeekSchedule(workingHours, BusinessSeed.Biz7Id,
                TimeSpan.FromHours(0), TimeSpan.FromHours(24),
                TimeSpan.FromHours(0), TimeSpan.FromHours(24), sundayOpen: true);

            await context.WorkingHours.AddRangeAsync(workingHours);
            await context.SaveChangesAsync();

            logger.LogInformation("WorkingHours seed edildi: {Count} kayıt", workingHours.Count);
        }

        private static void AddWeekSchedule(List<WorkingHour> list, Guid businessId,
            TimeSpan weekdayStart, TimeSpan weekdayEnd,
            TimeSpan saturdayStart, TimeSpan saturdayEnd,
            bool sundayOpen = false)
        {
            // Pazartesi-Cuma
            for (int day = 1; day <= 5; day++)
            {
                list.Add(new WorkingHour(Guid.NewGuid(), (DayOfWeek)day, weekdayStart, weekdayEnd, businessId));
            }

            // Cumartesi
            list.Add(new WorkingHour(Guid.NewGuid(), DayOfWeek.Saturday, saturdayStart, saturdayEnd, businessId));

            // Pazar
            if (sundayOpen)
            {
                list.Add(new WorkingHour(Guid.NewGuid(), DayOfWeek.Sunday, saturdayStart, saturdayEnd, businessId));
            }
            else
            {
                var closed = new WorkingHour(Guid.NewGuid(), DayOfWeek.Sunday, TimeSpan.FromHours(0), TimeSpan.FromHours(1), businessId);
                closed.Close();
                list.Add(closed);
            }
        }
    }
}