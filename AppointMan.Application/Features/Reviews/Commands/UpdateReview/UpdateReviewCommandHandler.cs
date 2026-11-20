using AppointMan.Application.Common.Abstractions;
using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
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
                // 1. Yorum kontrolu
                var review = await _reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken);
                if (review is null)
                    return Result<Guid>.Failure(Error.NotFound("Yorum", request.ReviewId.ToString()));

                // 2. Yetki kontrolu. Aktor kimligi imzali token'dan gelir;
                //    istek govdesindeki UserId tek basina guvenilmez.
                if (!CallerAuthorization.CanActAsUser(_currentUser, review.UserId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu yorumu guncelleme yetkiniz yok."));

                // 3. Yorum değişikliği ve puan yeniden hesaplaması aynı transaction'da.
                //    RecalculateAsync yorum veritabanına yazıldıktan sonra çalışmalı.
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
                // Beklenmeyen hata detayi istemciye sizdirilmaz.
                return Result<Guid>.Failure(Error.Internal("Yorum guncellenirken bir hata olustu."));
            }
        }

    }
}
