using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Reviews.Commands.UpdateReview
{
    public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, Result<Guid>>
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateReviewCommandHandler(
            IRepository<Review> reviewRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork)
        {
            _reviewRepository = reviewRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yorum kontrolu
                var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
                if (review is null)
                    return Result<Guid>.Failure(Error.NotFound("Yorum", request.ReviewId.ToString()));

                // 2. Yetki kontrolu
                if (review.UserId != request.UserId)
                    return Result<Guid>.Failure(Error.Unauthorized("Bu yorumu guncelleme yetkiniz yok."));

                // 3. Yorumu guncelle
                review.UpdateReview(request.Rating, request.Comment);

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
                return Result<Guid>.Failure(Error.Internal("Yorum guncellenirken bir hata olustu.", ex.Message));
            }
        }

        private async Task UpdateBusinessRating(Guid businessId, CancellationToken cancellationToken)
        {
            var reviews = await _reviewRepository.FindAsync(
                r => r.BusinessId == businessId && r.IsVisible && !r.IsDeleted,
                cancellationToken);

            if (!reviews.Any())
                return;

            var averageRating = reviews.Average(r => r.Rating);

            var business = await _businessRepository.GetByIdAsync(businessId, cancellationToken);
            if (business is not null)
            {
                business.UpdateRating((decimal)averageRating);
            }
        }
    }
}