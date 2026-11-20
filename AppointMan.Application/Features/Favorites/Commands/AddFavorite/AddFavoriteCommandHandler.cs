using AppointMan.Application.Common.Errors;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Favorites.Commands.AddFavorite
{
    public class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand, Result<Guid>>
    {
        private readonly IRepository<Favorite> _favoriteRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public AddFavoriteCommandHandler(
            IRepository<Favorite> favoriteRepository,
            IRepository<User> userRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _favoriteRepository = favoriteRepository;
            _userRepository = userRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 0. Favori yalnizca JWT kimligine ait kullanici icin olusturulabilir.
                //    Govde UserId'si yetki kaynagi degildir.
                if (!_currentUser.IsAuthenticated ||
                    _currentUser.UserId is null ||
                    _currentUser.UserId.Value != request.UserId)
                    return Result<Guid>.Failure(
                        Error.Unauthorized("Baska kullanici adina favori ekleme yetkiniz yok."));

                // 1. Kullanici kontrolu
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanici", request.UserId.ToString()));

                // 2. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 3. Idempotent kontrol: ayni cift icin aktif favori varsa yeni kayit
                //    olusturulmaz, mevcut kayit doner. Istemci tekrar denemesi 409 uretmez.
                var existingFavorites = await _favoriteRepository.FindAsync(
                    f => f.UserId == request.UserId && f.BusinessId == request.BusinessId && !f.IsDeleted,
                    cancellationToken);

                var existing = existingFavorites.FirstOrDefault();
                if (existing is not null)
                    return Result<Guid>.Success(existing.Id);

                // 4. Yeni kayit. (UserId, BusinessId) uzerindeki FILTRELI unique index
                //    yalnizca aktif kayitlari kapsar; soft-delete edilmis kayit yeniden
                //    eklemeyi engellemez.
                var favorite = new Favorite(request.UserId, request.BusinessId, request.Note);

                await _favoriteRepository.AddAsync(favorite, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(favorite.Id);
            }
            catch (DuplicateKeyException)
            {
                // Es zamanli iki ekleme denemesi: aktif kayit olustuysa idempotent don.
                var raced = await _favoriteRepository.FindAsync(
                    f => f.UserId == request.UserId && f.BusinessId == request.BusinessId && !f.IsDeleted,
                    CancellationToken.None);

                var winner = raced.FirstOrDefault();
                if (winner is not null)
                    return Result<Guid>.Success(winner.Id);

                return Result<Guid>.Failure(ConcurrencyErrors.DuplicateFavorite());
            }
            catch (ConcurrencyConflictException)
            {
                return Result<Guid>.Failure(ConcurrencyErrors.ConcurrencyConflict());
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Favori eklenirken bir hata olustu.", ex.Message));
            }
        }
    }
}
