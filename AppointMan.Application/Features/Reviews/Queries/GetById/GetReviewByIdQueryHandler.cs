using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Reviews.Queries.GetById
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
                // 1. Yorumu ID'ye gore getir
                var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);

                // 2. Yorum bulunamadiysa hata dondur
                if (review is null)
                    return Result<ReviewDto>.Failure(Error.NotFound("Yorum", request.ReviewId.ToString()));

                // 3. Gizli veya silinmis yorumlar sadece yazarina, isletme sahibine
                //    veya yoneticiye acik; digerlerine yokmus gibi davranilir.
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

                // 4. DTO'ya donustur
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
                // Beklenmeyen hata detayi istemciye sizdirilmaz.
                return Result<ReviewDto>.Failure(Error.Internal("Yorum bilgileri getirilirken bir hata olustu."));
            }
        }
    }
}
