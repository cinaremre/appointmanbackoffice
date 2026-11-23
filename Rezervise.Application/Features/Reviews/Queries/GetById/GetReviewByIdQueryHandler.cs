using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Reviews.Queries.GetById
{
    public class GetReviewByIdQueryHandler : IRequestHandler<GetReviewByIdQuery, Result<ReviewDto>>
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly ICurrentUserService _currentUser;

        public GetReviewByIdQueryHandler(
            IRepository<Review> reviewRepository,
            IRepository<Business> businessRepository,
            ICurrentUserService currentUser)
        {
            _reviewRepository = reviewRepository;
            _businessRepository = businessRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<ReviewDto>> Handle(GetReviewByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yorumu ID'ye göre getir
                var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);

                // 2. Yorum bulunamadıysa hata döndür
                if (review is null)
                    return Result<ReviewDto>.Failure(Error.NotFound("Yorum", request.ReviewId.ToString()));

                // 3. Gizli veya silinmiş yorumlar sadece yazarına, işletme sahibine
                //    veya yöneticiye açık; diğerlerine yokmus gibi davranılır.
                if (!review.IsVisible || review.IsDeleted)
                {
                    var canView = CallerAuthorization.CanActAsUser(_currentUser, review.UserId);

                    if (!canView)
                    {
                        var business = await _businessRepository.GetByIdAsync(review.BusinessId, cancellationToken);
                        canView = business is not null
                            && CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId);
                    }

                    if (!canView)
                        return Result<ReviewDto>.Failure(Error.NotFound("Yorum", request.ReviewId.ToString()));
                }

                // 4. DTO'ya dönüştür
                var dto = new ReviewDto
                {
                    Id = review.Id,
                    UserId = review.UserId,
                    BusinessId = review.BusinessId,
                    AppointmentId = review.AppointmentId,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    ReviewDate = review.ReviewDate,
                    IsVisible = review.IsVisible,
                    UserFullName = review.User?.GetFullName(),
                    UserEmail = review.User?.Email,
                    BusinessName = review.Business?.Name
                };

                return Result<ReviewDto>.Success(dto);
            }
            catch (Exception)
            {
                // Beklenmeyen hata detayı istemciye sızdırılmaz.
                return Result<ReviewDto>.Failure(Error.Internal("Yorum bilgileri getirilirken bir hata oluştu."));
            }
        }
    }
}
