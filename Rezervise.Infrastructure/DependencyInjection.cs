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
            // Atomik sayac guncellemesi (lost update korumasi)
            services.AddScoped<IBusinessRatingUpdater, BusinessRatingUpdater>();
            // Rezervasyon aralik kilidi (cift rezervasyon korumasi)
            services.AddScoped<IAppointmentBookingGuard, AppointmentBookingGuard>();
            // Domain event yan etkilerinin yapilandirilmis denetim kaydi
            services.AddScoped<IWorkflowAuditSink, LoggingWorkflowAuditSink>();
            // Ozellik erisimi: override > abonelik plani > varsayilan (Modul 14)
            services.AddScoped<IFeatureAccessService, FeatureAccessService>();
            // Backoffice yonetici denetim kayitlari (Modul 21)
            services.AddScoped<IAdminAuditSink, AdminAuditSink>();
            return services;
        }
    }
}