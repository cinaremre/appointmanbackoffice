using Rezervise.Application.Common.Interfaces;
using Rezervise.Domain.Interfaces;
using Rezervise.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rezervise.Infrastructure.Extensions
{
    /// <summary>
    /// Her iki hoşt'un (Mobil + BackOffice) birlikte kullanması gereken
    /// ortak altyapı kayıtları. Tek Application assembly'sını MediatR
    /// taradığı için handler'larının bütün bağımlılıkları her hoşt'ta
    /// çözülebilir olmalıdır — bu uzantı tek yerden sağlar; unutulamaz.
    ///
    /// Kullanım:
    ///   builder.Services.AddSharedInfrastructure(
    ///       builder.Configuration, builder.Environment.IsDevelopment());
    /// </summary>
    public static class SharedInfrastructureExtensions
    {
        /// <summary>
        /// E-posta, SMS, push, özellik erişimi ve yönetici denetimi tek yerden kaydeder.
        /// </summary>
        /// <param name="services">Service koleksiyonu.</param>
        /// <param name="configuration">Uygulama yapılandırması.</param>
        /// <param name="isDevelopment">Development ortamı mi (log modları belirler).</param>
        public static IServiceCollection AddSharedInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            bool isDevelopment)
        {
            // E-posta: SMTP yoksa log taşıması.
            services.AddEmailInfrastructure(configuration, isDevelopment);

            // SMS / OTP: sağlayıcı yoksa log taşıması.
            services.AddSmsInfrastructure(configuration, isDevelopment);

            // Push / FCM: service account yoksa log taşıması.
            services.AddPushInfrastructure(configuration);

            // Özellik erişimi (Modül 14): override > abonelik > varsayılan.
            services.AddScoped<IFeatureAccessService, FeatureAccessService>();

            // Yönetici denetim kaydı (Modül 21): her hoşt'ta çözülebilir olmalı.
            services.AddScoped<IAdminAuditSink, AdminAuditSink>();

            // IAdminAuditContext her hoşt tarafından kendi HttpContext
            // erişimcisiyle ayrı kaydedilir (backoffice: BackOffice
            // middleware'i; mobil: yerel AdminAuditContextAccessor).

            return services;
        }
    }
}
