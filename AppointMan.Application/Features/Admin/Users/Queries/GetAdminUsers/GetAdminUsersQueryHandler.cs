using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace AppointMan.Application.Features.Admin.Users.Queries.GetAdminUsers
{
    /// <summary>
    /// Kullanici listesini filtreler ve sayfalar (dokuman Modul 11).
    /// Arama alanimlari ad, soyad ve e-postadir; gizli alanlar donmez.
    /// </summary>
    public class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, Result<AdminPagedResult<AdminUserListItemDto>>>
    {
        /// <summary>Izin verilen en buyuk sayfa boyutu (dokuman 7.4).</summary>
        public const int MaxPageSize = 200;

        /// <summary>pageSize verilmediginde kullanilan varsayilan.</summary>
        public const int DefaultPageSize = 50;

        private readonly IRepository<User> _userRepository;

        public GetAdminUsersQueryHandler(IRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<AdminPagedResult<AdminUserListItemDto>>> Handle(
            GetAdminUsersQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sayfa degerleri tek yerden sinirlanir: sayfa en az 1,
                //    sayfa boyutu [1, MaxPageSize] araligina cekilir.
                var page = Math.Max(request.Page, 1);
                var pageSize = Math.Clamp(
                    request.PageSize <= 0 ? DefaultPageSize : request.PageSize,
                    1,
                    MaxPageSize);

                // 2. Filtreler ifade uzerinde kurulur; EF tarafi parametrelendirir.
                var search = Normalize(request.Search);
                var email = Normalize(request.Email);
                var isActive = request.IsActive;

                var users = await _userRepository.FindAsync(
                    BuildPredicate(search, email, isActive),
                    cancellationToken);

                // 3. En yeni kayit once olacak sekilde sirala ve sayfala.
                var ordered = users
                    .OrderByDescending(u => u.CreatedAt)
                    .ThenBy(u => u.Id)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(ToListItem)
                    .ToList();

                return Result.Success(new AdminPagedResult<AdminUserListItemDto>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = ordered.Count
                });
            }
            catch (Exception ex)
            {
                return Result.Failure<AdminPagedResult<AdminUserListItemDto>>(
                    Error.Internal("Kullanicilar listelenirken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>
        /// Arama, e-posta ve hesap durumu filtrelerini tek ifadeye toplar.
        /// Null filtreler kisayol degerlendirmesiyle atlanir.
        /// </summary>
        private static Expression<Func<User, bool>> BuildPredicate(
            string? search,
            string? email,
            bool? isActive)
            => u =>
                (search == null
                    || u.FirstName.ToLower().Contains(search)
                    || u.LastName.ToLower().Contains(search)
                    || u.Email.ToLower().Contains(search))
                && (email == null || u.Email.ToLower() == email)
                && (isActive == null
                    || (isActive.Value
                        ? u.AccountStatus == EnumAccountStatus.Active
                        : u.AccountStatus != EnumAccountStatus.Active));

        /// <summary>Filtre metnini kucuk harfe cevirir; bos ise null doner.</summary>
        private static string? Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

        /// <summary>Entity'yi gizli alanlari tasimayan liste ogesine cevirir.</summary>
        private static AdminUserListItemDto ToListItem(User user) => new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber?.Value,
            Role = user.Role,
            // Kilit bayragi iki kaynakta aranir: yonetici kilidi (AccountStatus)
            // ve brute force korumasindan kalan LockoutEndUtc.
            IsLockedOut = user.AccountStatus == EnumAccountStatus.Locked || user.IsLockedOut,
            CreatedAt = user.CreatedAt
        };
    }
}
