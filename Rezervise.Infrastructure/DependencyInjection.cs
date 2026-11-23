using Rezervise.Application.Common.Abstractions;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Domain.Interfaces;
using Rezervise.Infrastructure.Persistence.Context;
using Rezervise.Infrastructure.Persistence.Repositories;
using Rezervise.Infrastructure.Persistence.Services;
using Rezervise.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rezervise.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // DbContexts
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

            // UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IDateTimeService, DateTimeService>();
            // Generic Repository
            services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            // Atomik sayaç güncellemesi (lost update koruması)
            services.AddScoped<IBusinessRatingUpdater, BusinessRatingUpdater>();
            // Rezervasyon aralık kilidi (çift rezervasyon koruması)
            services.AddScoped<IAppointmentBookingGuard, AppointmentBookingGuard>();
            // Domain event yan etkilerinin yapılandırılmış denetim kaydı
            services.AddScoped<IWorkflowAuditSink, LoggingWorkflowAuditSink>();
            // Özellik erişimi: override > abonelik planı > varsayılan (Modül 14)
            services.AddScoped<IFeatureAccessService, FeatureAccessService>();
            // Backoffice yönetici denetim kayıtları (Modül 21)
            services.AddScoped<IAdminAuditSink, AdminAuditSink>();
            return services;
        }
    }
}