using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Users.Commands.UnlockAdminUser
{
    /// <summary>
    /// Kilili bir kullanici hesabini tekrar acar (dokuman Modul 11).
    /// Gerekce zorunludur ve denetim kaydina yazilir.
    /// </summary>
    public class UnlockAdminUserCommand : IRequest<Result>
    {
        public Guid UserId { get; set; }

        /// <summary>Yoneticinin acilis gerekcesi; zorunludur.</summary>
        public string? Reason { get; set; }
    }
}
