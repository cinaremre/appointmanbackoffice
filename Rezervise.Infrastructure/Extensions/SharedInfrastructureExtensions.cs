using Rezervise.Application.Common.Interfaces;
using Rezervise.Domain.Interfaces;
using Rezervise.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rezervise.Infrastructure.Extensions
{
    /// <summary>
    /// Her iki host'un (Mobil + BackOffice) birlikte kullanmasi gereken
    /// ortak altyapi kayitlari. Tek Application assembly'sini MediatR
    /// taradigi icin handler'larinin butun bagimliliklari her host'ta
    /// cozulebilir olmalidir — bu uzanti tek yerden saglar; unutulamaz.
    ///
    /// Kullanim:
    ///   builder.Services.AddSharedInfrastructure(
    ///       builder.Configuration, builder.Environment.IsDevelopment());
    /// </summary>
    public static class SharedInfrastructureExtensions
    {
        /// <summary>
        /// E-posta, SMS, push, ozellik erisimi ve yonetici denetimi tek yerden kaydeder.
        /// </summary>
        /// <param name="services">Service koleksiyonu.</param>
        /// <param name="configuration">Uygulama yapilandirmasi.</param>
        /// <param name="isDevelopment">Development ortami mi (log modlari belirler).</param>
        public static IServiceCollection AddSharedInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            bool isDevelopment)
        {
            // E-posta: SMTP yoksa log tasimasi.
            services.AddEmailInfrastructure(configuration, isDevelopment);

            // SMS / OTP: saglayici yoksa log tasimasi.
            services.AddSmsInfrastructure(configuration, isDevelopment);

            // Push / FCM: service account yoksa log tasimasi.
            services.AddPushInfrastructure(configuration);

            // Ozellik erisimi (Modul 14): override > abonelik > varsayilan.
            services.AddScoped<IFeatureAccessService, FeatureAccessService>();

            // Yonetici denetim kaydi (Modul 21): her host'ta cozulebilir olmali.
            services.AddScoped<IAdminAuditSink, AdminAuditSink>();

            // IAdminAuditContext her host tarafindan kendi HttpContext
            // erisimcisiyle ayri kaydedilir (backoffice: BackOffice
            // middleware'i; mobil: yerel AdminAuditContextAccessor).

            return services;
        }
    }
}
