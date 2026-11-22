using Rezervise.Application.Common.Behaviors;
using Rezervise.Application.Common.Diagnostics;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Services;
using Rezervise.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Rezervise.Application
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
            // Yinelenen event'lerin yan etki uretmesini engelleyen surec genelinde tek kayit.
            services.AddSingleton<EventSideEffectDeduplicator>();

            // Business metrics: a single singleton owns the Meter and its instruments.
            services.AddSingleton<RezerviseMetrics>();

            return services;
        }
    }
}