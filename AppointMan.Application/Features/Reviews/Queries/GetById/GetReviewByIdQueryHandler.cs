using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Reviews.Queries.GetById
{
    public class GetReviewByIdQueryHandler : IRequestHandler<GetReviewByIdQuery, Result<ReviewDto>>
    {
        private readonly IRepository<Review> _reviewRepository;

        public GetReviewByIdQueryHandler(IRepository<Review> reviewRepository)
        {
            _reviewRepository = reviewRepository;
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

                // 3. DTO'ya donustur
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
            catch (Exception ex)
            {
                return Result<ReviewDto>.Failure(Error.Internal("Yorum bilgileri getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}