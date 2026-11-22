using Rezervise.Application.Common.Abstractions;
using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Errors;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Reviews.Commands.AddReview
{
    public class AddReviewCommandHandler : IRequestHandler<AddReviewCommand, Result<Guid>>
    {
        private readonly IRepository<Review> _reviewRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IBusinessRatingUpdater _businessRatingUpdater;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public AddReviewCommandHandler(
            IRepository<Review> reviewRepository,
            IRepository<User> userRepository,
            IRepository<Business> businessRepository,
            IRepository<Appointment> appointmentRepository,
            IBusinessRatingUpdater businessRatingUpdater,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
            _businessRepository = businessRepository;
            _appointmentRepository = appointmentRepository;
            _businessRatingUpdater = businessRatingUpdater;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(AddReviewCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 0. Aktor kimligi her zaman imzali token'dan gelir; govde UserId'si
                //    tek basina yetki kaynagi degildir (IDOR korumasi).
                if (!CallerAuthorization.CanActAsUser(_currentUser, request.UserId))
                    return Result<Guid>.Failure(Error.Unauthorized("Baska kullanici adina yorum ekleme yetkiniz yok."));

                // 1. Kullanici kontrolu
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanici", request.UserId.ToString()));

                // 2. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 3. Randevu kontrolu (zorunlu)
                if (request.AppointmentId is null)
                    return Result<Guid>.Failure(Error.Validation("Randevu ID zorunludur."));

                var appointmentId = request.AppointmentId.Value;
                var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, cancellationToken);
                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFound("Randevu", appointmentId.ToString()));

                // Kullanici randevuya ait mi?
                if (appointment.UserId != request.UserId)
                    return Result<Guid>.Failure(Error.BusinessRule("Bu randevuya yorum yapma yetkiniz yok."));

                // Randevu, yorum yapilan isletmeye ait mi?
                if (appointment.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.BusinessRule("Randevu, yorum yapilan isletmeye ait degil."));

                // Randevu tamamlanmis mi?
                if (appointment.Status != Domain.Enums.EnumAppointmentStatus.Completed)
                    return Result<Guid>.Failure(Error.BusinessRule("Sadece tamamlanmis randevulara yorum yapilabilir."));

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

                // 6-7. Yorum kaydi ve atomik puan güncellemesi ayni transaction'da.
                //      Read-modify-write yerine tek UPDATE kullanilir; bir adim
                //      basarisiz olursa yorum da geri alinir.
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    await _reviewRepository.AddAsync(review, token);
                    await _unitOfWork.SaveChangesAsync(token);
                    await _businessRatingUpdater.UpdateOnNewReviewAsync(
                        request.BusinessId, request.Rating, token);
                }, cancellationToken: cancellationToken);

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
            catch (Exception)
            {
                // Beklenmeyen hata detayi istemciye sizdirilmaz.
                return Result<Guid>.Failure(Error.Internal("Yorum eklenirken bir hata olustu."));
            }
        }
    }
}
