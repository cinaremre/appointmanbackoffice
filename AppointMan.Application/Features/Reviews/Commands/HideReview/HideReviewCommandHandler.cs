using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Reviews.Commands.HideReview
{
    public class HideReviewCommandHandler : IRequestHandler<HideReviewCommand, Result<Guid>>
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public HideReviewCommandHandler(
            IRepository<Review> reviewRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _reviewRepository = reviewRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(HideReviewCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 0. Controller'daki rol kisitinin yaninda handler seviyesinde de
                //    yonetici zorunlulugu dogrulanir (defense in depth).
                if (!CallerAuthorization.IsAdministrator(_currentUser))
                    return Result<Guid>.Failure(Error.Forbidden("Yorumu gizleme yetkiniz yok."));

                // 1. Yorum kontrolu
                var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
                if (review is null)
                    return Result<Guid>.Failure(Error.NotFound("Yorum", request.ReviewId.ToString()));

                // 2. Yorum zaten gizli mi?
                if (!review.IsVisible)
                    return Result<Guid>.Failure(Error.Conflict("Yorum zaten gizli."));

                // 3. Gizle
                review.Hide();

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
            catch (Exception)
            {
                // Beklenmeyen hata detayi istemciye sizdirilmaz.
                return Result<Guid>.Failure(Error.Internal("Yorum gizlenirken bir hata olustu."));
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
