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

                // 3. Zaten favori mi?
                var existingFavorites = await _favoriteRepository.FindAsync(
                    f => f.UserId == request.UserId && f.BusinessId == request.BusinessId && !f.IsDeleted,
                    cancellationToken);

                if (existingFavorites.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu isletme zaten favorilerde."));

                // 4. Favori olustur
                var favorite = new Favorite(request.UserId, request.BusinessId, request.Note);

                // 5. Kaydet
                await _favoriteRepository.AddAsync(favorite, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(favorite.Id);
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