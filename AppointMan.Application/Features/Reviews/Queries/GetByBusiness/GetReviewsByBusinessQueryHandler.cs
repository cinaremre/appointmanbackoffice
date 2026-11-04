using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Reviews.Queries.GetByBusiness
{
    public class GetReviewsByBusinessQueryHandler : IRequestHandler<GetReviewsByBusinessQuery, Result<List<ReviewDto>>>
    {
        private readonly IRepository<Review> _reviewRepository;

        public GetReviewsByBusinessQueryHandler(IRepository<Review> reviewRepository)
        {
            _reviewRepository = reviewRepository;
        }

        public async Task<Result<List<ReviewDto>>> Handle(GetReviewsByBusinessQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletmenin gorunur yorumlarini getir
                var reviews = await _reviewRepository.FindAsync(
                    r => r.BusinessId == request.BusinessId && r.IsVisible && !r.IsDeleted,
                    cancellationToken);

                // 2. Sayfalama
                var pagedReviews = reviews
                    .OrderByDescending(r => r.ReviewDate)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 3. DTO'ya donustur
                var dtos = pagedReviews.Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    BusinessId = r.BusinessId,
                    AppointmentId = r.AppointmentId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ReviewDate = r.ReviewDate,
                    IsVisible = r.IsVisible,
                    UserFullName = r.User?.GetFullName(),
                    UserEmail = r.User?.Email,
                    BusinessName = r.Business?.Name
                }).ToList();

                return Result<List<ReviewDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<ReviewDto>>.Failure(
                    Error.Internal("Yorumlar getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}