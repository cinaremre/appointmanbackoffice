using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Businesses.Queries.GetMyBusinesses
{
    /// <summary>
    /// Isletme sahibinin kendi isletmelerini listeler. Sahiplik kosulu
    /// sorgunun kendisinde tasir (OwnerId == currentUser.UserId); baska
    /// kullanilarin isletmeleri hicbir kosulda sonuca giremez.
    /// </summary>
    public class GetMyBusinessesQueryHandler
        : IRequestHandler<GetMyBusinessesQuery, Result<AdminPagedResult<OwnerBusinessListItemDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyBusinessesQueryHandler(
            IRepository<Business> businessRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<AdminPagedResult<OwnerBusinessListItemDto>>> Handle(
            GetMyBusinessesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kimlik cozumlenmisse owner portali hicbir sey acamaz.
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<AdminPagedResult<OwnerBusinessListItemDto>>.Failure(
                        Error.Unauthorized("Auth.Unauthorized"));

                var ownerId = _currentUser.UserId.Value;

                // 2. Sayfalama kurallari backoffice ortak sinirlarindan gelir.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                // 3. Sahibe ait, silinmemis isletmeler; ad sirali.
                var businesses = await _businessRepository.FindAsync(
                    b => b.OwnerId == ownerId && !b.IsDeleted,
                    cancellationToken);

                var ordered = businesses
                    .OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(b => b.Id)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new OwnerBusinessListItemDto
                    {
                        Id = b.Id,
                        Name = b.Name,
                        Description = b.Description,
                        IsActive = b.IsActive,
                        Rating = b.Rating,
                        TotalReviews = b.TotalReviews,
                        CityId = b.Address?.CityId ?? 0,
                        CreatedAt = b.CreatedAt
                    })
                    .ToList();

                return Result<AdminPagedResult<OwnerBusinessListItemDto>>.Success(
                    new AdminPagedResult<OwnerBusinessListItemDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<OwnerBusinessListItemDto>>.Failure(
                    Error.Internal("Isletme listesi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
