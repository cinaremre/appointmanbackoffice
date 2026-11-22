using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Users.Queries.GetAdminUsers
{
    /// <summary>
    /// Backoffice kullanici listesi sorgusu (dokuman Modul 11).
    /// Filtreler: serbest metin arama (ad/soyad/e-posta icerir), e-posta,
    /// hesap durumu. Sifre ve hash alanlari asla dondurulmez.
    /// </summary>
    public class GetAdminUsersQuery : IRequest<Result<AdminPagedResult<AdminUserListItemDto>>>
    {
        /// <summary>
        /// Serbest metin arama; FirstName, LastName ve Email alanlarinda
        /// buyuk/kucuk harf duyarsiz "icerir" eslesmesi yapar.
        /// </summary>
        public string? Search { get; set; }

        /// <summary>Tam e-posta eslesmesi (buyuk/kucuk harf duyarsiz).</summary>
        public string? Email { get; set; }

        /// <summary>true: yalnizca aktif hesaplar; false: aktif olmayanlar.</summary>
        public bool? IsActive { get; set; }

        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler tarafinda en fazla 200'e sinirlanir.</summary>
        public int PageSize { get; set; } = 50;
    }
}
