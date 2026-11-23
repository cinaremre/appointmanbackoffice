using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Users.Queries.GetAdminUserDetail
{
    /// <summary>
    /// Tek bir kullanıcıyı işletme üyelikleri ve son giriş bilgisiyle getirir
    /// (doküman Modül 11). Şifre/hash alanları dönmez.
    /// </summary>
    public class GetAdminUserDetailQuery : IRequest<Result<AdminUserDetailDto>>
    {
        public Guid UserId { get; set; }
    }
}
