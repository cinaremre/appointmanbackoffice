using System.Security.Claims;
using Rezervise.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Rezervise.BackOffice.Api.Extensions
{
    /// <summary>
    /// Backoffice istekleri için <see cref="ICurrentUserService"/> kaydı.
    /// Program.cs dokunulmadan bırakıldığı için ayrı bir dosyada sunulur;
    /// hoşt'ta tek satırlık bağlantı gerektirir:
    /// builder.Services.AddBackOfficeCurrentUser();
    /// </summary>
    public static class CurrentUserServiceExtensions
    {
        /// <summary>
        /// HttpContext claim'lerinden okunan yönetici kimliğini kaydeder.
        /// Backoffice JWT'lerinde rol claim tipi "role" olarak çözülür.
        /// </summary>
        public static IServiceCollection AddBackOfficeCurrentUser(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, BackOfficeCurrentUserService>();
            return services;
        }
    }

    /// <summary>
    /// İmzalı token'dan yönetici kimliğini çözer. Kimlik asla istek
    /// gövdesinden değil, yalnızca claim'lerden türetilir.
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

        /// <inheritdoç />
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

        /// <inheritdoç />
        public bool IsAuthenticated
            => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

        /// <inheritdoç />
        public IReadOnlyCollection<string> Roles
        {
            get
            {
                var principal = _httpContextAccessor.HttpContext?.User;

                if (principal is null)
                    return Array.Empty<string>();

                // Backoffice tökenlarında rol claim tipi "role" olarak ayarlanır;
                // bazı istemciler varsayılan ClaimTypes.Role da gönderebilir.
                return principal.FindAll("role")
                    .Concat(principal.FindAll(ClaimTypes.Role))
                    .Select(claim => claim.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }

        /// <inheritdoç />
        public bool IsInRole(string role)
            => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
