using Rezervise.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Rezervise.BackOffice.Api.Extensions
{
    /// <summary>
    /// Permission tabanli yetki altyapisi (dokuman 5.3). Her policy bir
    /// permission koduyla eslesir; cozumleme AdminPermissions matrisinden
    /// yapilir. SuperAdmin her permission'a sahiptir.
    /// </summary>
    public static class PermissionPolicyExtensions
    {
        /// <summary>
        /// Tanimli permission policy'lerini kaydeder. Controller'larda
        /// [Authorize(Policy = AdminPermissions.X)] biciminde kullanilir.
        /// </summary>
        public static IServiceCollection AddBackOfficeAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                Register(options, AdminPermissions.BusinessRead);
                Register(options, AdminPermissions.BusinessCreate);
                Register(options, AdminPermissions.BusinessUpdate);
                Register(options, AdminPermissions.BusinessSuspend);
                Register(options, AdminPermissions.CategoryRead);
                Register(options, AdminPermissions.CategoryCreate);
                Register(options, AdminPermissions.CategoryUpdate);
                Register(options, AdminPermissions.CategoryArchive);
                Register(options, AdminPermissions.ServiceRead);
                Register(options, AdminPermissions.ServiceUpdate);
                Register(options, AdminPermissions.ServiceBulkUpdate);
                Register(options, AdminPermissions.AppointmentRead);
                Register(options, AdminPermissions.AppointmentCancel);
                Register(options, AdminPermissions.AppointmentOverride);
                Register(options, AdminPermissions.UserRead);
                Register(options, AdminPermissions.UserManage);
                Register(options, AdminPermissions.SubscriptionRead);
                Register(options, AdminPermissions.SubscriptionUpdate);
                Register(options, AdminPermissions.FeatureRead);
                Register(options, AdminPermissions.FeatureOverride);
                Register(options, AdminPermissions.FinanceRead);
                Register(options, AdminPermissions.NotificationRead);
                Register(options, AdminPermissions.NotificationRetry);
                Register(options, AdminPermissions.MarketingRead);
                Register(options, AdminPermissions.MarketingManage);
                Register(options, AdminPermissions.StockRead);
                Register(options, AdminPermissions.PackageRead);
                Register(options, AdminPermissions.ReportRead);
                Register(options, AdminPermissions.AuditRead);
                Register(options, AdminPermissions.OperationsRead);
                Register(options, AdminPermissions.OperationsManage);
            });

            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            return services;
        }

        /// <summary>Permission koduna policy kaydeder; policy adi = permission kodu.</summary>
        private static void Register(AuthorizationOptions options, string permission)
            => options.AddPolicy(permission, policy => policy.Requirements.Add(new PermissionRequirement(permission)));

        /// <summary>Tek bir permission kodunun istenmesi.</summary>
        private sealed class PermissionRequirement : IAuthorizationRequirement
        {
            public string Permission { get; }

            public PermissionRequirement(string permission)
                => Permission = permission;
        }

        /// <summary>
        /// JWT rollerini AdminPermissions matrisi uzerinden cozer.
        /// Token'da permission claim'i aranmaz: rol -> permission eslemesi
        /// sunucu tarafinda tek kaynaktan yurutulur.
        /// </summary>
        private sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
        {
            protected override Task HandleRequirementAsync(
                AuthorizationHandlerContext context,
                PermissionRequirement requirement)
            {
                if (context.User.Identity?.IsAuthenticated != true)
                    return Task.CompletedTask;

                // Claim calismasi: her rol icin matris sorgulanir; SuperAdmin her izne sahiptir.
                if (context.User.Claims
                    .Where(c => string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.Type, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", StringComparison.Ordinal))
                    .Select(c => c.Value)
                    .Any(role => AdminPermissions.HasPermission(role, requirement.Permission)))
                {
                    context.Succeed(requirement);
                }

                return Task.CompletedTask;
            }
        }
    }
}
