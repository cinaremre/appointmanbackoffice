using AppointMan.Application.Common.Interfaces;

namespace AppointMan.Application.Common.Authorization
{
    /// <summary>
    /// JWT uzerinden cozulen ambient kimlik icin ortak yetki kontrolleri.
    /// Istek govdesindeki kullanici ID'leri bu kontrollerin yerine gecmez.
    /// </summary>
    public static class CallerAuthorization
    {
        private static readonly string[] AdministratorRoles = { "Admin", "SuperAdmin" };

        /// <summary>Caller yonetici rollerinden birine sahip mi?</summary>
        public static bool IsAdministrator(ICurrentUserService currentUser)
            => AdministratorRoles.Any(currentUser.IsInRole);

        /// <summary>
        /// Caller kimligi dogrulanmis ve ya kendisi ya da yonetici mi?
        /// </summary>
        public static bool CanActAsUser(ICurrentUserService currentUser, Guid userId)
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return false;

            return IsAdministrator(currentUser) || currentUser.UserId.Value == userId;
        }

        /// <summary>
        /// Caller kimligi dogrulanmis ve verilen isletmenin sahibi veya yonetici mi?
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
