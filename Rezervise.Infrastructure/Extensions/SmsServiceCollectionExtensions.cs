using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Infrastructure.Services.Sms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Extensions
{
    /// <summary>
    /// Telefon doğrulama (OTP) hattının kayıt yardımcıları.
    /// SMS sağlayıcısı henüz bağlanmadığı için log tabanlı taşıma seçilir.
    /// </summary>
    public static class SmsServiceCollectionExtensions
    {
        /// <summary>
        /// OTP ayarlarını, kod servisini ve SMS taşımasını kaydeder.
        /// </summary>
        /// <param name="services">Servis koleksiyonu.</param>
        /// <param name="configuration">Uygulama yapılandırması ("Otp" bölümü).</param>
        /// <param name="isDevelopment">Ortam Development mi; kod gövdesinin loğlanmasını belirler.</param>
        /// <returns>Zincirleme için aynı koleksiyon.</returns>
        /// <exception cref="ArgumentNullException">Zorunlu argüman null öldüğünda fırlatılır.</exception>
        public static IServiceCollection AddSmsInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            bool isDevelopment)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            var options = OtpOptions.FromConfiguration(configuration);
            services.AddSingleton(options);

            services.AddSingleton<IOtpCodeService, OtpCodeService>();

            // Kod gövdesi yalnızca Development ortamında loğlanır.
            services.AddSingleton<ISmsCodeSender>(sp => new LogSmsCodeSender(
                sp.GetRequiredService<ILogger<LogSmsCodeSender>>(),
                isDevelopment));

            return services;
        }
    }
}
