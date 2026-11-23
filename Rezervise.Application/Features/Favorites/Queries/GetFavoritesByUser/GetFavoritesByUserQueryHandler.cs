using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Favorites.Queries.GetByUser
{
    public class GetFavoritesByUserQueryHandler : IRequestHandler<GetFavoritesByUserQuery, Result<List<FavoriteDto>>>
    {
        private readonly IRepository<Favorite> _favoriteRepository;

        public GetFavoritesByUserQueryHandler(IRepository<Favorite> favoriteRepository)
        {
            _favoriteRepository = favoriteRepository;
        }

        public async Task<Result<List<FavoriteDto>>> Handle(GetFavoritesByUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kullanıcının favorilerini getir.
                //    Business ve adresi include edilmezse DTO'daki BusinessName ve BusinessAddress
                //    null kalır. Address bir navigation öldüğü için iç ice yol gerekiyor.
                var favorites = await _favoriteRepository.FindWithIncludesAsync(
                    f => f.UserId == request.UserId && !f.IsDeleted,
                    new[]
                    {
                        nameof(Favorite.Business),
                        "Business.Address"
                    },
                    cancellationToken);

                // 2. Sayfalama
                var pagedFavorites = favorites
                    .OrderByDescending(f => f.AddedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 3. DTO'ya dönüştür
                var dtos = pagedFavorites.Select(f => new FavoriteDto
                {
                    Id = f.Id,
                    UserId = f.UserId,
                    BusinessId = f.BusinessId,
                    BusinessName = f.Business?.Name,
                    BusinessAddress = f.Business?.Address?.GetFullAddress(),
                    Note = f.Note,
                    AddedAt = f.AddedAt
                }).ToList();

                return Result<List<FavoriteDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<FavoriteDto>>.Failure(
                    Error.Internal("Favoriler getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}