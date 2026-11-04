using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Favorites.Commands.RemoveFavorite
{
    public class RemoveFavoriteCommand : IRequest<Result<Guid>>
    {
        public Guid UserId { get; set; }
        public Guid BusinessId { get; set; }
    }
}