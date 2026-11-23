using Rezervise.Application.Common.Errors;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Favorites.Commands.AddFavorite
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
                // 0. Favori yalnızca JWT kimliğine ait kullanıcı için oluşturulabilir.
                //    Gövde UserId'si yetki kaynağı değildir.
                if (!_currentUser.IsAuthenticated ||
                    _currentUser.UserId is null ||
                    _currentUser.UserId.Value != request.UserId)
                    return Result<Guid>.Failure(
                        Error.Unauthorized("Başka kullanıcı adına favori ekleme yetkiniz yok."));

                // 1. Kullanıcı kontrolü
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanıcı", request.UserId.ToString()));

                // 2. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 3. Idempotent kontrol: aynı çift için aktif favori varsa yeni kayıt
                //    oluşturulmaz, mevcut kayıt döner. İstemci tekrar denemesi 409 üretmez.
                var existingFavorites = await _favoriteRepository.FindAsync(
                    f => f.UserId == request.UserId && f.BusinessId == request.BusinessId && !f.IsDeleted,
                    cancellationToken);

                var existing = existingFavorites.FirstOrDefault();
                if (existing is not null)
                    return Result<Guid>.Success(existing.Id);

                // 4. Yeni kayıt. (UserId, BusinessId) üzerindeki FILTRELI unique index
                //    yalnızca aktif kayıtları kapsar; soft-delete edilmiş kayıt yeniden
                //    eklemeyi engellemez.
                var favorite = new Favorite(request.UserId, request.BusinessId, request.Note);

                await _favoriteRepository.AddAsync(favorite, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(favorite.Id);
            }
            catch (DuplicateKeyException)
            {
                // Es zamanlı iki ekleme denemesi: aktif kayıt oluştuysa idempotent dön.
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
                return Result<Guid>.Failure(Error.Internal("Favori eklenirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
