using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Infrastructure.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Extensions
{
    /// <summary>
    /// E-posta hattının kayıt yardımcıları. Taşıma, başlangıçta seçilir:
    /// Development değilse ve SMTP ayarları doluysa SMTP, aksi halde log modu.
    /// </summary>
    public static class EmailServiceCollectionExtensions
    {
        /// <summary>
        /// E-posta kuyruğunu ve taşımasını kaydeder. Yapılandırma bozuksa bile
        /// uygulama açılışta durmaz; log moduna düşer.
        /// </summary>
        /// <param name="services">Servis koleksiyonu.</param>
        /// <param name="configuration">Uygulama yapılandırması ("Email" bölümü).</param>
        /// <param name="isDevelopment">Ortam Development mi.</param>
        /// <returns>Zincirleme için aynı koleksiyon.</returns>
        /// <exception cref="ArgumentNullException">Zorunlu argüman null öldüğünda fırlatılır.</exception>
        public static IServiceCollection AddEmailInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            bool isDevelopment)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            var options = EmailOptions.FromConfiguration(configuration);
            services.AddSingleton(options);

            // SMTP yalnızca gerçekten yapılandırılmışsa ve Development değilse kullanılır.
            var useSmtp = !isDevelopment
                          && options.HasSmtpConfiguration
                          && !string.IsNullOrWhiteSpace(options.FromAddress);

            if (useSmtp)
            {
                services.AddSingleton<IEmailService, SmtpEmailService>();
            }
            else
            {
                // Gövde yalnızca Development ortamında loğlanır; aksi halde token gizlenir.
                services.AddSingleton<IEmailService>(sp => new LogEmailService(
                    sp.GetRequiredService<ILogger<LogEmailService>>(),
                    isDevelopment));
            }

            // Aynı örnek hem üretici hem tüketici arayüzlerini karşılar.
            services.AddSingleton<EmailQueue>();
            services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<EmailQueue>());
            services.AddSingleton<IEmailQueueReader>(sp => sp.GetRequiredService<EmailQueue>());

            return services;
        }
    }
}
