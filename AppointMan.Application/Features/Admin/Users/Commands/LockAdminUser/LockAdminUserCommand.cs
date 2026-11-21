using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Users.Commands.LockAdminUser
{
    /// <summary>
    /// Bir kullanicinin hesabini kilitler (dokuman Modul 11). Gerekce zorunludur
    /// ve denetim kaydina yazilir.
    /// </summary>
    public class LockAdminUserCommand : IRequest<Result>
    {
        public Guid UserId { get; set; }

        /// <summary>Yoneticinin kilit gerekcesi; zorunludur.</summary>
        public string? Reason { get; set; }
    }
}
