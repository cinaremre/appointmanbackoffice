using Rezervise.Application.Common.Interfaces;

namespace Rezervise.Application.Common.Authorization
{
    /// <summary>
    /// JWT üzerinden çözülen ambient kimlik için ortak yetki kontrolleri.
    /// İstek gövdesindeki kullanıcı ID'leri bu kontrollerin yerine geçmez.
    /// </summary>
    public static class CallerAuthorization
    {
        private static readonly string[] AdministratorRoles = { "Admin", "SuperAdmin" };

        /// <summary>Caller yönetici rollerinden birine sahip mi?</summary>
        public static bool IsAdministrator(ICurrentUserService currentUser)
            => AdministratorRoles.Any(currentUser.IsInRole);

        /// <summary>
        /// Caller kimliği doğrulanmış ve ya kendisi ya da yönetici mi?
        /// </summary>
        public static bool CanActAsUser(ICurrentUserService currentUser, Guid userId)
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return false;

            return IsAdministrator(currentUser) || currentUser.UserId.Value == userId;
        }

        /// <summary>
        /// Caller kimliği doğrulanmış ve verilen işletmenin sahibi veya yönetici mi?
        /// </summary>
        public static bool CanActForBusiness(
            ICurrentUserService currentUser,
            Guid ownerId,
            Func<bool>? isOwner = null)
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return false;

            return IsAdministrator(currentUser)
                || currentUser.UserId.Value == ownerId
                || (isOwner?.Invoke() ?? false);
        }
    }
}
