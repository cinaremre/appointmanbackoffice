using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Reviews.Commands.ShowReview
{
    public class ShowReviewCommandHandler : IRequestHandler<ShowReviewCommand, Result<Guid>>
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ShowReviewCommandHandler(
            IRepository<Review> reviewRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork)
        {
            _reviewRepository = reviewRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(ShowReviewCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yorum kontrolu
                var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
                if (review is null)
                    return Result<Guid>.Failure(Error.NotFound("Yorum", request.ReviewId.ToString()));

                // 2. Yorum zaten gorunur mu?
                if (review.IsVisible)
                    return Result<Guid>.Failure(Error.Conflict("Yorum zaten gorunur."));

                // 3. Goster
                review.Show();

                // 4. Isletme puanini guncelle
                await UpdateBusinessRating(review.BusinessId, cancellationToken);

                // 5. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(review.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Yorum gosterilirken bir hata olustu.", ex.Message));
            }
        }

        private async Task UpdateBusinessRating(Guid businessId, CancellationToken cancellationToken)
        {
            var reviews = await _reviewRepository.FindAsync(
                r => r.BusinessId == businessId && r.IsVisible && !r.IsDeleted,
                cancellationToken);

            var business = await _businessRepository.GetByIdAsync(businessId, cancellationToken);
            if (business is not null)
            {
                if (!reviews.Any())
                {
                    business.UpdateRating(0);
                }
                else
                {
                    var averageRating = reviews.Average(r => r.Rating);
                    business.UpdateRating((decimal)averageRating);
                }
            }
        }
    }
}