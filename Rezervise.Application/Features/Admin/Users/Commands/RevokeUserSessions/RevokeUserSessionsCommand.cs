using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Users.Commands.RevokeUserSessions
{
    /// <summary>
    /// Bir kullanıcının tüm oturumlarını geçersiz kılar (doküman Modül 11):
    /// refresh token hash'leri temizlenir, böylece mevcut token yenilenemez.
    /// Gerekçe zorunludur ve denetim kaydına yazılır.
    /// </summary>
    public class RevokeUserSessionsCommand : IRequest<Result>
    {
        public Guid UserId { get; set; }

        /// <summary>Yöneticinin oturum iptali gerekçesi; zorunludur.</summary>
        public string? Reason { get; set; }
    }
}
