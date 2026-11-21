using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Users.Commands.RevokeUserSessions
{
    /// <summary>
    /// Bir kullanicinin tum oturumlarini gecersiz kilar (dokuman Modul 11):
    /// refresh token hash'leri temizlenir, boylece mevcut token yenilenemez.
    /// Gerekce zorunludur ve denetim kaydina yazilir.
    /// </summary>
    public class RevokeUserSessionsCommand : IRequest<Result>
    {
        public Guid UserId { get; set; }

        /// <summary>Yoneticinin oturum iptali gerekcesi; zorunludur.</summary>
        public string? Reason { get; set; }
    }
}
