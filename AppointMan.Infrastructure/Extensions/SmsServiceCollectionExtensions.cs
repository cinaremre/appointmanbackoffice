using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Models;
using AppointMan.Infrastructure.Services.Sms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppointMan.Infrastructure.Extensions
{
    /// <summary>
    /// Telefon dogrulama (OTP) hattinin kayit yardimcilari.
    /// SMS saglayicisi henuz baglanmadigi icin log tabanli tasima secilir.
    /// </summary>
    public static class SmsServiceCollectionExtensions
    {
        /// <summary>
        /// OTP ayarlarini, kod servisini ve SMS tasimasini kaydeder.
        /// </summary>
        /// <param name="services">Servis koleksiyonu.</param>
        /// <param name="configuration">Uygulama yapilandirmasi ("Otp" bolumu).</param>
        /// <param name="isDevelopment">Ortam Development mi; kod govdesinin loglanmasini belirler.</param>
        /// <returns>Zincirleme icin ayni koleksiyon.</returns>
        /// <exception cref="ArgumentNullException">Zorunlu arguman null oldugunda firlatilir.</exception>
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

            // Kod govdesi yalnizca Development ortaminda loglanir.
            services.AddSingleton<ISmsCodeSender>(sp => new LogSmsCodeSender(
                sp.GetRequiredService<ILogger<LogSmsCodeSender>>(),
                isDevelopment));

            return services;
        }
    }
}
