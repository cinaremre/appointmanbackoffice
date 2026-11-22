namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Ambient access to the identity that initiated the current request.
    /// Handlers must use it instead of trusting user identifiers supplied by the caller.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>Identifier of the authenticated user; <c>null</c> for anonymous requests.</summary>
        Guid? UserId { get; }

        /// <summary>True when the current request carries an authenticated principal.</summary>
        bool IsAuthenticated { get; }

        /// <summary>Role names assigned to the current principal.</summary>
        IReadOnlyCollection<string> Roles { get; }

        /// <summary>
        /// Checks whether the current principal belongs to the given role.
        /// </summary>
        /// <param name="role">Role name to test.</param>
        /// <returns><c>true</c> when the principal is in the role.</returns>
        bool IsInRole(string role);
    }
}
