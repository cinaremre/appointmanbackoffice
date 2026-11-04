using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Favorites.Queries.GetByUser
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
                // 1. Kullanicinin favorilerini getir
                var favorites = await _favoriteRepository.FindAsync(
                    f => f.UserId == request.UserId && !f.IsDeleted,
                    cancellationToken);

                // 2. Sayfalama
                var pagedFavorites = favorites
                    .OrderByDescending(f => f.AddedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 3. DTO'ya donustur
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
                    Error.Internal("Favoriler getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}