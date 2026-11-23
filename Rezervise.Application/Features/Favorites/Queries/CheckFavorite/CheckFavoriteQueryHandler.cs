using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Favorites.Queries.CheckFavorite
{
    public class CheckFavoriteQueryHandler : IRequestHandler<CheckFavoriteQuery, Result<bool>>
    {
        private readonly IRepository<Favorite> _favoriteRepository;

        public CheckFavoriteQueryHandler(IRepository<Favorite> favoriteRepository)
        {
            _favoriteRepository = favoriteRepository;
        }

        public async Task<Result<bool>> Handle(CheckFavoriteQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Favori var mi kontrol et
                var favorites = await _favoriteRepository.FindAsync(
                    f => f.UserId == request.UserId && f.BusinessId == request.BusinessId && !f.IsDeleted,
                    cancellationToken);

                var isFavorite = favorites.Any();

                return Result<bool>.Success(isFavorite);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(
                    Error.Internal("Favori kontrolü yapılırken bir hata oluştu.", ex.Message));
            }
        }
    }
}