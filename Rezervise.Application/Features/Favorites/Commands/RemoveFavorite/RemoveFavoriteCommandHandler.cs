using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Favorites.Commands.RemoveFavorite
{
    public class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand, Result<Guid>>
    {
        private readonly IRepository<Favorite> _favoriteRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public RemoveFavoriteCommandHandler(
            IRepository<Favorite> favoriteRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _favoriteRepository = favoriteRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 0. Favori yalnızca kendi sahibi tarafından silinebilir.
                //    Gövde UserId'si yetki kaynağı değildir.
                if (!_currentUser.IsAuthenticated ||
                    _currentUser.UserId is null ||
                    _currentUser.UserId.Value != request.UserId)
                    return Result<Guid>.Failure(
                        Error.Unauthorized("Başka kullanıcıya ait favoriyi silme yetkiniz yok."));

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
                return Result<Guid>.Failure(Error.Internal("Favori silinirken bir hata oluştu.", ex.Message));
            }
        }
    }
}