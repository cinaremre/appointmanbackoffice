using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Favorites.Queries.CheckFavorite
{
    public class CheckFavoriteQuery : IRequest<Result<bool>>
    {
        public Guid UserId { get; set; }
        public Guid BusinessId { get; set; }
    }
}