using Rezervise.Application.Common.Abstractions;
using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Reviews.Commands.ShowReview
{
    public class ShowReviewCommandHandler : IRequestHandler<ShowReviewCommand, Result<Guid>>
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IBusinessRatingUpdater _ratingUpdater;

        public ShowReviewCommandHandler(
            IRepository<Review> reviewRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IBusinessRatingUpdater ratingUpdater)
        {
            _reviewRepository = reviewRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _ratingUpdater = ratingUpdater;
        }

        public async Task<Result<Guid>> Handle(ShowReviewCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 0. Controller'daki rol kısıtının yanında handler seviyesinde de
                //    yönetici zorunluluğu doğrulanır (defense in depth).
                if (!CallerAuthorization.IsAdministrator(_currentUser))
                    return Result<Guid>.Failure(Error.Forbidden("Yorumu gösterme yetkiniz yok."));

                // 1. Yorum kontrolü
                var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
                if (review is null)
                    return Result<Guid>.Failure(Error.NotFound("Yorum", request.ReviewId.ToString()));

                // 2. Yorum zaten görünür mü?
                if (review.IsVisible)
                    return Result<Guid>.Failure(Error.Conflict("Review.AlreadyVisible"));

                // 3. Gösterme ve puan yeniden hesaplaması aynı transaction'da.
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    review.Show();
                    await _unitOfWork.SaveChangesAsync(token);
                    await _ratingUpdater.RecalculateAsync(review.BusinessId, token);
                }, cancellationToken: cancellationToken);

                return Result<Guid>.Success(review.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception)
            {
                // Beklenmeyen hata detayı istemciye sızdırılmaz.
                return Result<Guid>.Failure(Error.Internal("Yorum gösterilirken bir hata oluştu."));
            }
        }

    }
}
