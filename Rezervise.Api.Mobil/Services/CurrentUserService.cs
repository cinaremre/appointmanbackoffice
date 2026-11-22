using System.Security.Claims;
using Rezervise.Application.Common.Interfaces;

namespace Rezervise.Api.Services
{
    /// <summary>
    /// <see cref="ICurrentUserService"/> implementation backed by the current HTTP context claims.
    /// Authorization decisions must be derived from the signed token, never from the request body.
    /// </summary>
    public sealed class CurrentUserService : ICurrentUserService
    {
        private const string UserIdClaimType = "userId";

        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initialises the service with an HTTP context accessor.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor used to read the current principal.</param>
        /// <exception cref="ArgumentNullException">Thrown when the accessor is null.</exception>
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
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

                return principal.FindAll(ClaimTypes.Role)
                    .Select(claim => claim.Value)
                    .ToArray();
            }
        }

        /// <inheritdoc />
        public bool IsInRole(string role)
            => !string.IsNullOrWhiteSpace(role)
               && Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
