using Rezervise.Application.Common.Abstractions;
using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Reviews.Commands.UpdateReview
{
    public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, Result<Guid>>
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IBusinessRatingUpdater _ratingUpdater;

        public UpdateReviewCommandHandler(
            IRepository<Review> reviewRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IBusinessRatingUpdater ratingUpdater)
        {
            _reviewRepository = reviewRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _ratingUpdater = ratingUpdater;
        }

        public async Task<Result<Guid>> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yorum kontrolü
                var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
                if (review is null)
                    return Result<Guid>.Failure(Error.NotFound("Yorum", request.ReviewId.ToString()));

                // 2. Yetki kontrolü. Aktör kimliği imzalı token'dan gelir;
                //    istek gövdesindeki UserId tek basına güvenilmez.
                if (!CallerAuthorization.CanActAsUser(_currentUser, review.UserId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu yorumu güncelleme yetkiniz yok."));

                // 3. Yorum değişikliği ve puan yeniden hesaplaması aynı transaction'da.
                //    RecalculateAsync yorum veritabanına yazıldıktan sonra çalismali.
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    review.UpdateReview(request.Rating, request.Comment);
                    await _unitOfWork.SaveChangesAsync(token);
                    await _ratingUpdater.RecalculateAsync(review.BusinessId, token);
                }, cancellationToken: cancellationToken);

                return Result<Guid>.Success(review.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception)
            {
                // Beklenmeyen hata detayı istemciye sızdırılmaz.
                return Result<Guid>.Failure(Error.Internal("Yorum güncellenirken bir hata oluştu."));
            }
        }

    }
}
