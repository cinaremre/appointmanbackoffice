using Rezervise.Domain.Enums;

namespace Rezervise.Application.Common.Security
{
    /// <summary>
    /// Roles a self service registration is allowed to obtain.
    /// Everything outside this whitelist (Admin, SuperAdmin, Moderator, Support, Employee)
    /// must be assigned by an administrator, never by the caller.
    /// </summary>
    public static class SelfAssignableRoles
    {
        /// <summary>Roles a public registration endpoint may assign.</summary>
        public static IReadOnlyCollection<EnumUserRole> All { get; } = new[]
        {
            EnumUserRole.Customer,
            EnumUserRole.BusinessOwner
        };

        /// <summary>
        /// Determines whether the requested role may be granted during self service registration.
        /// </summary>
        /// <param name="role">Role requested by the caller.</param>
        /// <returns><c>true</c> when the role is part of the whitelist.</returns>
        public static bool IsSelfAssignable(EnumUserRole role)
            => role is EnumUserRole.Customer or EnumUserRole.BusinessOwner;
    }
}
