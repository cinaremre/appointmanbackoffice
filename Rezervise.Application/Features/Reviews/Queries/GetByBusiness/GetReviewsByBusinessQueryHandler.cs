using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Reviews.Queries.GetByBusiness
{
    public class GetReviewsByBusinessQueryHandler : IRequestHandler<GetReviewsByBusinessQuery, Result<List<ReviewDto>>>
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly ICurrentUserService _currentUser;

        public GetReviewsByBusinessQueryHandler(
            IRepository<Review> reviewRepository,
            IRepository<Business> businessRepository,
            ICurrentUserService currentUser)
        {
            _reviewRepository = reviewRepository;
            _businessRepository = businessRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<List<ReviewDto>>> Handle(GetReviewsByBusinessQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Gizli yorumlar sadece isletme sahibi veya yoneticiye acik.
                //    Silinmis yorumlar hicbir zaman listelenmez.
                var canSeeHidden = CallerAuthorization.IsAdministrator(_currentUser);
                if (!canSeeHidden)
                {
                    var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                    canSeeHidden = business is not null
                        && CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId);
                }

                // 2. Isletmenin gorunur yorumlarini getir
                var reviews = await _reviewRepository.FindAsync(
                    r => r.BusinessId == request.BusinessId
                         && !r.IsDeleted
                         && (r.IsVisible || canSeeHidden),
                    cancellationToken);

                // 3. Sayfalama
                var pagedReviews = reviews
                    .OrderByDescending(r => r.ReviewDate)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 4. DTO'ya donustur
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
