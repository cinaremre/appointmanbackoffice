using AppointMan.Application.Common.Behaviors;
using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Services;
using AppointMan.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AppointMan.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            // FluentValidation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // TokenService
            services.AddScoped<ITokenService, TokenService>();

            // Domain event'leri kalici kayit sonrasi MediatR ile yayinlar.
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            // Business metrics: a single singleton owns the Meter and its instruments.
            services.AddSingleton<AppointManMetrics>();

            return services;
        }
    }
}