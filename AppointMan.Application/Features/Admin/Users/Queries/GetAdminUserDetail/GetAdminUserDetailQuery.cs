using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Users.Queries.GetAdminUserDetail
{
    /// <summary>
    /// Tek bir kullaniciyi isletme uyelikleri ve son giris bilgisiyle getirir
    /// (dokuman Modul 11). Sifre/hash alanlari donmez.
    /// </summary>
    public class GetAdminUserDetailQuery : IRequest<Result<AdminUserDetailDto>>
    {
        public Guid UserId { get; set; }
    }
}
