using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Users.Commands.UnlockAdminUser
{
    /// <summary>
    /// Kilili bir kullanıcı hesabını tekrar açar (doküman Modül 11).
    /// Gerekçe zorunludur ve denetim kaydına yazılır.
    /// </summary>
    public class UnlockAdminUserCommand : IRequest<Result>
    {
        public Guid UserId { get; set; }

        /// <summary>Yöneticinin açılış gerekçesi; zorunludur.</summary>
        public string? Reason { get; set; }
    }
}
