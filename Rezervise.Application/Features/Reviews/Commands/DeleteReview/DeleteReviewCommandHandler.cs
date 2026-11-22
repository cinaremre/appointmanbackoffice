using Rezervise.Application.Common.Abstractions;
using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Reviews.Commands.DeleteReview
{
    public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, Result<Guid>>
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IBusinessRatingUpdater _ratingUpdater;

        public DeleteReviewCommandHandler(
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

        public async Task<Result<Guid>> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yorum kontrolu
                var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
                if (review is null)
                    return Result<Guid>.Failure(Error.NotFound("Yorum", request.ReviewId.ToString()));

                // 2. Yetki kontrolu. Aktor kimligi imzali token'dan gelir;
                //    istek govdesindeki UserId tek basina guvenilmez.
                if (!CallerAuthorization.CanActAsUser(_currentUser, review.UserId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu yorumu silme yetkiniz yok."));

                // 3. Silme ve puan yeniden hesaplamasi ayni transaction'da.
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    review.Delete();
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
                // Beklenmeyen hata detayi istemciye sizdirilmaz.
                return Result<Guid>.Failure(Error.Internal("Yorum silinirken bir hata olustu."));
            }
        }

    }
}
