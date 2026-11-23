using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Users.Commands.LockAdminUser
{
    /// <summary>
    /// Bir kullanıcının hesabını kilitler (doküman Modül 11). Gerekçe zorunludur
    /// ve denetim kaydına yazılır.
    /// </summary>
    public class LockAdminUserCommand : IRequest<Result>
    {
        public Guid UserId { get; set; }

        /// <summary>Yöneticinin kilit gerekçesi; zorunludur.</summary>
        public string? Reason { get; set; }
    }
}
