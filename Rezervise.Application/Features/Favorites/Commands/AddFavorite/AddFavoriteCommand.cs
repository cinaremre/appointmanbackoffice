using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Favorites.Commands.AddFavorite
{
    public class AddFavoriteCommand : IRequest<Result<Guid>>
    {
        public Guid UserId { get; set; }
        public Guid BusinessId { get; set; }
        public string? Note { get; set; }
    }
}