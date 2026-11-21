using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Common;
using AppointMan.Application.Features.Owner.Common;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Owner.Reviews.Queries.GetMyReviews
{
    /// <summary>
    /// Isletme sahibinin yorum listesi. Sahiplik OwnerScope ile
    /// dogrulanir; yorumlar en yeni once, gizli yorumlar dahil listelenir
    /// (owner tum durumlari gormelidir).
    /// </summary>
    public class GetMyReviewsQueryHandler
        : IRequestHandler<GetMyReviewsQuery, Result<AdminPagedResult<OwnerReviewDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Review> _reviewRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyReviewsQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Review> reviewRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _reviewRepository = reviewRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<AdminPagedResult<OwnerReviewDto>>> Handle(
            GetMyReviewsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahiplik: isletme JWT sahibine ait olmali.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<AdminPagedResult<OwnerReviewDto>>.Failure(owned.Error!);

                // 2. Sayfalama kurallari.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                // 3. Isletmenin silinmemis yorumlari (gizli dahil).
                var reviews = await _reviewRepository.FindAsync(
                    r => r.BusinessId == request.BusinessId && !r.IsDeleted,
                    cancellationToken);

                // 4. En yeni yorum once; karisikligi onlemek icin Id sirasi.
                var ordered = reviews
                    .OrderByDescending(r => r.CreatedAt)
                    .ThenBy(r => r.Id)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new OwnerReviewDto
                    {
                        Id = r.Id,
                        CustomerId = r.UserId,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        IsHidden = !r.IsVisible,
                        CreatedAt = r.CreatedAt
                    })
                    .ToList();

                return Result<AdminPagedResult<OwnerReviewDto>>.Success(
                    new AdminPagedResult<OwnerReviewDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<OwnerReviewDto>>.Failure(
                    Error.Internal("Yorum listesi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
