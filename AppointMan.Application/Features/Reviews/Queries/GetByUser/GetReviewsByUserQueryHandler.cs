using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Reviews.Queries.GetByUser
{
    public class GetReviewsByUserQueryHandler : IRequestHandler<GetReviewsByUserQuery, Result<List<ReviewDto>>>
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly ICurrentUserService _currentUser;

        public GetReviewsByUserQueryHandler(
            IRepository<Review> reviewRepository,
            ICurrentUserService currentUser)
        {
            _reviewRepository = reviewRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<List<ReviewDto>>> Handle(GetReviewsByUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 0. Aktor kimligi imzali token'dan gelir; baskasinin yorumlari
                //    sadece yonetici tarafindan goruntulenebilir (IDOR korumasi).
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<List<ReviewDto>>.Failure(Error.Unauthorized("Auth.InvalidUser"));

                if (!CallerAuthorization.CanActAsUser(_currentUser, request.UserId))
                    return Result<List<ReviewDto>>.Failure(Error.Forbidden("Baska kullanicinin yorumlarini goruntuleme yetkiniz yok."));

                // 1. Kullanicinin tum yorumlarini getir
                var reviews = await _reviewRepository.FindAsync(
                    r => r.UserId == request.UserId && !r.IsDeleted,
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
            catch (Exception)
            {
                // Beklenmeyen hata detayi istemciye sizdirilmaz.
                return Result<List<ReviewDto>>.Failure(
                    Error.Internal("Yorumlar getirilirken bir hata olustu."));
            }
        }
    }
}
