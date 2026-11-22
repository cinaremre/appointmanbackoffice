using System.Security.Claims;
using Rezervise.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Rezervise.BackOffice.Api.Extensions
{
    /// <summary>
    /// Backoffice istekleri icin <see cref="ICurrentUserService"/> kaydi.
    /// Program.cs dokunulmadan birakildigi icin ayri bir dosyada sunulur;
    /// host'ta tek satirlik baglanti gerektirir:
    /// builder.Services.AddBackOfficeCurrentUser();
    /// </summary>
    public static class CurrentUserServiceExtensions
    {
        /// <summary>
        /// HttpContext claim'lerinden okunan yonetici kimligini kaydeder.
        /// Backoffice JWT'lerinde rol claim tipi "role" olarak cozulur.
        /// </summary>
        public static IServiceCollection AddBackOfficeCurrentUser(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, BackOfficeCurrentUserService>();
            return services;
        }
    }

    /// <summary>
    /// Imzali token'dan yonetici kimligini cozer. Kimlik asla istek
    /// govdesinden degil, yalnizca claim'lerden turetilir.
    /// </summary>
    public sealed class BackOfficeCurrentUserService : ICurrentUserService
    {
        private const string UserIdClaimType = "userId";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public BackOfficeCurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor
                ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <inheritdoc />
        public Guid? UserId
        {
            get
            {
                var principal = _httpContextAccessor.HttpContext?.User;

                if (principal?.Identity?.IsAuthenticated != true)
                    return null;

                var rawUserId = principal.FindFirst(UserIdClaimType)?.Value
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return Guid.TryParse(rawUserId, out var userId) ? userId : null;
            }
        }

        /// <inheritdoc />
        public bool IsAuthenticated
            => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

        /// <inheritdoc />
        public IReadOnlyCollection<string> Roles
        {
            get
            {
                var principal = _httpContextAccessor.HttpContext?.User;

                if (principal is null)
                    return Array.Empty<string>();

                // Backoffice tokenlarinda rol claim tipi "role" olarak ayarlanir;
                // bazi istemciler varsayilan ClaimTypes.Role da gonderebilir.
                return principal.FindAll("role")
                    .Concat(principal.FindAll(ClaimTypes.Role))
                    .Select(claim => claim.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }

        /// <inheritdoc />
        public bool IsInRole(string role)
            => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
