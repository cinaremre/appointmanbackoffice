using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Users.Queries.GetAdminUsers
{
    /// <summary>
    /// Backoffice kullanıcı listesi sorgusu (doküman Modül 11).
    /// Filtreler: serbest metin arama (ad/soyad/e-posta içerir), e-posta,
    /// hesap durumu. Şifre ve hash alanları asla döndürülmez.
    /// </summary>
    public class GetAdminUsersQuery : IRequest<Result<AdminPagedResult<AdminUserListItemDto>>>
    {
        /// <summary>
        /// Serbest metin arama; FirstName, LastName ve Email alanlarında
        /// büyük/küçük harf duyarsız "içerir" eşleşmesi yapar.
        /// </summary>
        public string? Search { get; set; }

        /// <summary>Tam e-posta eşleşmesi (büyük/küçük harf duyarsız).</summary>
        public string? Email { get; set; }

        /// <summary>true: yalnızca aktif hesaplar; false: aktif olmayanlar.</summary>
        public bool? IsActive { get; set; }

        /// <summary>1 tabanlı sayfa numarası.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler tarafında en fazla 200'e sınırlanır.</summary>
        public int PageSize { get; set; } = 50;
    }
}
