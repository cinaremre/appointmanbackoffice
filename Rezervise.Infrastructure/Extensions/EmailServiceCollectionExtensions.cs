using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Infrastructure.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Extensions
{
    /// <summary>
    /// E-posta hattinin kayit yardimcilari. Tasima, baslangicta secilir:
    /// Development degilse ve SMTP ayarlari doluysa SMTP, aksi halde log modu.
    /// </summary>
    public static class EmailServiceCollectionExtensions
    {
        /// <summary>
        /// E-posta kuyrugunu ve tasimasini kaydeder. Yapilandirma bozuksa bile
        /// uygulama acilista durmaz; log moduna duser.
        /// </summary>
        /// <param name="services">Servis koleksiyonu.</param>
        /// <param name="configuration">Uygulama yapilandirmasi ("Email" bolumu).</param>
        /// <param name="isDevelopment">Ortam Development mi.</param>
        /// <returns>Zincirleme icin ayni koleksiyon.</returns>
        /// <exception cref="ArgumentNullException">Zorunlu arguman null oldugunda firlatilir.</exception>
        public static IServiceCollection AddEmailInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            bool isDevelopment)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            var options = EmailOptions.FromConfiguration(configuration);
            services.AddSingleton(options);

            // SMTP yalnizca gercekten yapilandirilmissa ve Development degilse kullanilir.
            var useSmtp = !isDevelopment
                          && options.HasSmtpConfiguration
                          && !string.IsNullOrWhiteSpace(options.FromAddress);

            if (useSmtp)
            {
                services.AddSingleton<IEmailService, SmtpEmailService>();
            }
            else
            {
                // Govde yalnizca Development ortaminda loglanir; aksi halde token gizlenir.
                services.AddSingleton<IEmailService>(sp => new LogEmailService(
                    sp.GetRequiredService<ILogger<LogEmailService>>(),
                    isDevelopment));
            }

            // Ayni ornek hem uretici hem tuketici arayuzlerini karsilar.
            services.AddSingleton<EmailQueue>();
            services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<EmailQueue>());
            services.AddSingleton<IEmailQueueReader>(sp => sp.GetRequiredService<EmailQueue>());

            return services;
        }
    }
}
