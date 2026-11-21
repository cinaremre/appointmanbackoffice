using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Models;
using AppointMan.Infrastructure.Services.Push;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppointMan.Infrastructure.Extensions
{
    /// <summary>
    /// Push bildirim hattinin kayit yardimcilari (SMS/e-posta desenleri ile uyumlu).
    /// </summary>
    public static class PushServiceCollectionExtensions
    {
        /// <summary>
        /// FCM ayarlarini, push kuyrugunu ve secilen tasimayi kaydeder.
        /// Yapilandirma tam degilse log tasimasi kullanilir; uygulama asla patlamaz.
        /// </summary>
        public static IServiceCollection AddPushInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            var settings = PushSettings.FromConfiguration(configuration);
            services.AddSingleton(settings);

            // Kuyruk: tek instance hem yazar hem okur tarafina servis edilir.
            services.AddSingleton<PushQueue>();
            services.AddSingleton<IPushQueue>(sp => sp.GetRequiredService<PushQueue>());
            services.AddSingleton<IPushQueueReader>(sp => sp.GetRequiredService<PushQueue>());

            if (settings.HasFcmConfiguration)
            {
                services.AddSingleton<IPushNotificationSender, FcmPushNotificationSender>();
            }
            else
            {
                services.AddSingleton<IPushNotificationSender>(sp =>
                    new LogPushNotificationSender(
                        sp.GetRequiredService<ILogger<LogPushNotificationSender>>()));
            }

            return services;
        }
    }
}
