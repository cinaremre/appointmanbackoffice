using AppointMan.Application.Common.Errors;
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

        public AddFavoriteCommandHandler(
            IRepository<Favorite> favoriteRepository,
            IRepository<User> userRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork)
        {
            _favoriteRepository = favoriteRepository;
            _userRepository = userRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kullanici kontrolu
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanici", request.UserId.ToString()));

                // 2. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 3. On kontrol YAPILMAZ: (UserId, BusinessId) uzerindeki unique index
                //    tek dogruluk kaynagidir. Es zamanli iki ekleme denemesinde
                //    ikincisi DuplicateKeyException alir ve 409 Conflict doner.
                var favorite = new Favorite(request.UserId, request.BusinessId, request.Note);

                await _favoriteRepository.AddAsync(favorite, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(favorite.Id);
            }
            catch (DuplicateKeyException)
            {
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
