using AppointMan.Application.Common.Abstractions;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Domain.Interfaces;
using AppointMan.Infrastructure.Persistence.Context;
using AppointMan.Infrastructure.Persistence.Repositories;
using AppointMan.Infrastructure.Persistence.Services;
using AppointMan.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppointMan.Infrastructure
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
            return services;
        }
    }
}