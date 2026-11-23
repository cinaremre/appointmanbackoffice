using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Infrastructure.Services.Push;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Extensions
{
    /// <summary>
    /// Push bildirim hattının kayıt yardımcıları (SMS/e-posta desenleri ile uyumlu).
    /// </summary>
    public static class PushServiceCollectionExtensions
    {
        /// <summary>
        /// FCM ayarlarını, push kuyruğunu ve seçilen taşımayı kaydeder.
        /// Yapılandırma tam değilse log taşıması kullanılır; uygulama asla patlamaz.
        /// </summary>
        public static IServiceCollection AddPushInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            var settings = PushSettings.FromConfiguration(configuration);
            services.AddSingleton(settings);

            // Kuyruk: tek instance hem yazar hem okur tarafına servis edilir.
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
