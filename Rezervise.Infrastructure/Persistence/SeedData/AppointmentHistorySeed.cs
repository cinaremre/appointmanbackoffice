using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    public static class AppointmentHistorySeed
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.AppointmentHistories.AnyAsync())
            {
                logger.LogInformation("AppointmentHistories zaten mevcut, atlaniyor.");
                return;
            }

            var today = DateTime.UtcNow.Date;
            var histories = new List<AppointmentHistory>();

            // Apt1: Pending -> Confirmed -> Completed
            histories.Add(CreateHistory(AppointmentSeed.Apt1Id, EnumAppointmentStatus.Pending,
                EnumAppointmentStatus.Confirmed, "Randevu onaylandi",
                today.AddDays(-12).AddHours(2), "businessowner1@example.com"));

            histories.Add(CreateHistory(AppointmentSeed.Apt1Id, EnumAppointmentStatus.Confirmed,
                EnumAppointmentStatus.Completed, "Randevu tamamlandi",
                today.AddDays(-10).AddHours(11), "businessowner1@example.com"));

            // Apt2: Pending -> Confirmed -> Completed
            histories.Add(CreateHistory(AppointmentSeed.Apt2Id, EnumAppointmentStatus.Pending,
                EnumAppointmentStatus.Confirmed, "Randevu onaylandi",
                today.AddDays(-10).AddHours(3), "businessowner2@example.com"));

            histories.Add(CreateHistory(AppointmentSeed.Apt2Id, EnumAppointmentStatus.Confirmed,
                EnumAppointmentStatus.Completed, "Randevu tamamlandi",
                today.AddDays(-8).AddHours(15), "businessowner2@example.com"));

            // Apt3: Pending -> Confirmed -> Cancelled
            histories.Add(CreateHistory(AppointmentSeed.Apt3Id, EnumAppointmentStatus.Pending,
                EnumAppointmentStatus.Confirmed, "Randevu onaylandi",
                today.AddDays(-7).AddHours(4), "businessowner2@example.com"));

            histories.Add(CreateHistory(AppointmentSeed.Apt3Id, EnumAppointmentStatus.Confirmed,
                EnumAppointmentStatus.Cancelled, "Hasta gelemedi",
                today.AddDays(-5).AddHours(10), "businessowner2@example.com"));

            // Apt4: Pending -> Confirmed -> Completed
            histories.Add(CreateHistory(AppointmentSeed.Apt4Id, EnumAppointmentStatus.Pending,
                EnumAppointmentStatus.Confirmed, "Randevu onaylandi",
                today.AddDays(-5).AddHours(1), "businessowner3@example.com"));

            histories.Add(CreateHistory(AppointmentSeed.Apt4Id, EnumAppointmentStatus.Confirmed,
                EnumAppointmentStatus.Completed, "Randevu tamamlandi",
                today.AddDays(-3).AddHours(16), "businessowner3@example.com"));

            // Apt5: Pending -> Confirmed -> Completed
            histories.Add(CreateHistory(AppointmentSeed.Apt5Id, EnumAppointmentStatus.Pending,
                EnumAppointmentStatus.Confirmed, "Randevu onaylandi",
                today.AddDays(-4).AddHours(2), "businessowner1@example.com"));

            histories.Add(CreateHistory(AppointmentSeed.Apt5Id, EnumAppointmentStatus.Confirmed,
                EnumAppointmentStatus.Completed, "Randevu tamamlandi",
                today.AddDays(-2).AddHours(14), "businessowner1@example.com"));

            // Apt6: Pending -> Confirmed
            histories.Add(CreateHistory(AppointmentSeed.Apt6Id, EnumAppointmentStatus.Pending,
                EnumAppointmentStatus.Confirmed, "Randevu onaylandi",
                today.AddDays(-1).AddHours(5), "businessowner1@example.com"));

            // Apt7: Pending -> Confirmed
            histories.Add(CreateHistory(AppointmentSeed.Apt7Id, EnumAppointmentStatus.Pending,
                EnumAppointmentStatus.Confirmed, "Randevu onaylandi",
                today.AddDays(-1).AddHours(6), "businessowner2@example.com"));

            // Apt9: Pending -> Confirmed
            histories.Add(CreateHistory(AppointmentSeed.Apt9Id, EnumAppointmentStatus.Pending,
                EnumAppointmentStatus.Confirmed, "Randevu onaylandi",
                today.AddDays(-1).AddHours(7), "businessowner3@example.com"));

            await context.AppointmentHistories.AddRangeAsync(histories);
            await context.SaveChangesAsync();

            logger.LogInformation("AppointmentHistories seed edildi: {Count} kayit", histories.Count);
        }

        private static AppointmentHistory CreateHistory(Guid appointmentId,
            EnumAppointmentStatus oldStatus, EnumAppointmentStatus newStatus,
            string reason, DateTime changedAt, string changedBy)
        {
            var history = new AppointmentHistory(Guid.NewGuid(), appointmentId, oldStatus, newStatus, changedBy, reason);
            history.SetChangedAtForSeed(changedAt);
            return history;
        }
    }
}