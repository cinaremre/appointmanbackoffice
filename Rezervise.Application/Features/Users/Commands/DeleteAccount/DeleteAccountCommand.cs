using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.DeleteAccount
{
    /// <summary>
    /// Erases the authenticated user's personal data and soft deletes the account.
    /// The actor is never part of the request: it is resolved from the access token.
    /// </summary>
    public class DeleteAccountCommand : IRequest<Result<bool>>
    {
        /// <summary>
        /// Current password, required to re-authenticate this destructive action.
        /// </summary>
        public string CurrentPassword { get; set; }

        /// <summary>
        /// Optional free-text reason supplied by the client. Accepted for audit context
        /// but deliberately NOT persisted (no column, therefore no migration) and NOT
        /// logged, because free text may itself contain personal data.
        /// </summary>
        public string? Reason { get; set; }
    }
}
