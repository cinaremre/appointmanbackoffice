using AppointMan.Application.Common.Abstractions;
using AppointMan.Application.Common.Errors;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Reviews.Commands.AddReview
{
    public class AddReviewCommandHandler : IRequestHandler<AddReviewCommand, Result<Guid>>
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IBusinessRatingUpdater _businessRatingUpdater;
        private readonly IUnitOfWork _unitOfWork;

        public AddReviewCommandHandler(
            IRepository<Review> reviewRepository,
            IRepository<User> userRepository,
            IRepository<Business> businessRepository,
            IRepository<Appointment> appointmentRepository,
            IBusinessRatingUpdater businessRatingUpdater,
            IUnitOfWork unitOfWork)
        {
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
            _businessRepository = businessRepository;
            _appointmentRepository = appointmentRepository;
            _businessRatingUpdater = businessRatingUpdater;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(AddReviewCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kullanici kontrolu
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanici", request.UserId.ToString()));

                // 2. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 3. Randevu kontrolu (opsiyonel)
                if (request.AppointmentId.HasValue)
                {
                    var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId.Value, cancellationToken);
                    if (appointment is null)
                        return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.Value.ToString()));

                    // Kullanici randevuya ait mi?
                    if (appointment.UserId != request.UserId)
                        return Result<Guid>.Failure(Error.BusinessRule("Bu randevuya yorum yapma yetkiniz yok."));

                    // Randevu tamamlanmis mi?
                    if (appointment.Status != Domain.Enums.EnumAppointmentStatus.Completed)
                        return Result<Guid>.Failure(Error.BusinessRule("Sadece tamamlanmis randevulara yorum yapilabilir."));
                }

                // 4. Ayni randevu icin ikinci yorum on kontrolu YAPILMAZ:
                //    UX_Reviews_AppointmentId unique index tek dogruluk kaynagidir.
                //    Es zamanli iki denemede ikincisi DuplicateKeyException alir.

                // 5. Yorum olustur
                var review = new Review(
                    request.UserId,
                    request.BusinessId,
                    request.Rating,
                    request.Comment,
                    request.AppointmentId);

                // 6. Kaydet (unique index ihlali burada yakalanir)
                await _reviewRepository.AddAsync(review, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 7. Isletme puanini atomik olarak guncelle.
                //    Read-modify-write yerine tek UPDATE kullanildigi icin
                //    es zamanli yorumlarda sayac kaybi olusmaz.
                await _businessRatingUpdater.UpdateOnNewReviewAsync(
                    request.BusinessId, request.Rating, cancellationToken);

                return Result<Guid>.Success(review.Id);
            }
            catch (DuplicateKeyException)
            {
                return Result<Guid>.Failure(ConcurrencyErrors.DuplicateReview());
            }
            catch (ConcurrencyConflictException)
            {
                return Result<Guid>.Failure(ConcurrencyErrors.ConcurrencyConflict());
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Yorum eklenirken bir hata olustu.", ex.Message));
            }
        }
    }
}
