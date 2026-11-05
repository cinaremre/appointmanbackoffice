using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppointMan.Infrastructure.Persistence.SeedData
{
    public static class NotificationSeed
    {
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.Notifications.AnyAsync())
            {
                logger.LogInformation("Notifications zaten mevcut, atlaniyor.");
                return;
            }

            var now = DateTime.UtcNow;
            var today = now.Date;
            var notifications = new List<Notification>();

            // Ahmet'e randevu hatirlatmasi
            var n1 = new Notification(Guid.NewGuid(), UserSeed.Customer1Id,
                "Randevu Hatirlatmasi",
                "Yarin saat 10:00da Kadikoy Berberinde randevunuz var.",
                EnumNotificationType.AppointmentReminder,
                "/appointments/" + AppointmentSeed.Apt6Id);
            n1.SetSentAtForSeed(now.AddHours(-12));
            notifications.Add(n1);

            // Zeynep'e randevu onayi
            var n2 = new Notification(Guid.NewGuid(), UserSeed.Customer2Id,
                "Randevunuz Onaylandi",
                "Besiktas Kuaforindeki randevunuz onaylandi.",
                EnumNotificationType.AppointmentConfirmation,
                "/appointments/" + AppointmentSeed.Apt7Id);
            n2.SetSentAtForSeed(today.AddDays(-1));
            n2.MarkAsRead();
            notifications.Add(n2);

            // Can'a randevu iptali
            var n3 = new Notification(Guid.NewGuid(), UserSeed.Customer3Id,
                "Randevunuz Iptal Edildi",
                "Alsancak Dis Klinigindeki randevunuz iptal edildi.",
                EnumNotificationType.AppointmentCancellation,
                "/appointments/" + AppointmentSeed.Apt3Id);
            n3.SetSentAtForSeed(today.AddDays(-5));
            n3.MarkAsRead();
            notifications.Add(n3);

            // Elif'e yorum istegi
            var n4 = new Notification(Guid.NewGuid(), UserSeed.Customer4Id,
                "Deneyiminizi Puanlayin",
                "Taksim Tattoo dakineyiminiz nasildi?",
                EnumNotificationType.ReviewRequest,
                "/reviews/create?appointmentId=" + AppointmentSeed.Apt4Id);
            n4.SetSentAtForSeed(today.AddDays(-1));
            notifications.Add(n4);

            // Mehmet'e promosyon
            var n5 = new Notification(Guid.NewGuid(), UserSeed.Owner1Id,
                "Yeni Kampanya!",
                "Kis sezonu indirimleri basladi! Tum hizmetlerde %20 indirim.",
                EnumNotificationType.Promotional,
                "/promotions/winter2026");
            n5.SetSentAtForSeed(today.AddDays(-2));
            notifications.Add(n5);

            // Ali'ye isletme dogrulama
            var n6 = new Notification(Guid.NewGuid(), UserSeed.Owner3Id,
                "Isletmeniz Dogrulandi",
                "Tebrikler! Taksim Tattoo Studio dogrulandi.",
                EnumNotificationType.BusinessVerification,
                "/businesses/" + BusinessSeed.Biz3Id);
            n6.SetSentAtForSeed(today.AddDays(-30));
            n6.MarkAsRead();
            notifications.Add(n6);

            await context.Notifications.AddRangeAsync(notifications);
            await context.SaveChangesAsync();

            logger.LogInformation("Notifications seed edildi: {Count} bildirim", notifications.Count);
        }
    }
}