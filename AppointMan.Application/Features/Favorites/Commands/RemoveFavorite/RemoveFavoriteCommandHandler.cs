using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Favorites.Commands.RemoveFavorite
{
    public class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand, Result<Guid>>
    {
        private readonly IRepository<Favorite> _favoriteRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveFavoriteCommandHandler(
            IRepository<Favorite> favoriteRepository,
            IUnitOfWork unitOfWork)
        {
            _favoriteRepository = favoriteRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Favoriyi bul
                var favorites = await _favoriteRepository.FindAsync(
                    f => f.UserId == request.UserId && f.BusinessId == request.BusinessId && !f.IsDeleted,
                    cancellationToken);

                var favorite = favorites.FirstOrDefault();

                if (favorite is null)
                    return Result<Guid>.Failure(Error.NotFound("Favori", $"User:{request.UserId}, Business:{request.BusinessId}"));

                // 2. Soft delete
                favorite.Delete();

                // 3. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(favorite.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Favori silinirken bir hata olustu.", ex.Message));
            }
        }
    }
}