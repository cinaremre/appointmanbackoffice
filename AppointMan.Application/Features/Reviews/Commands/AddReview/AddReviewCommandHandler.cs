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
        private readonly IUnitOfWork _unitOfWork;

        public AddReviewCommandHandler(
            IRepository<Review> reviewRepository,
            IRepository<User> userRepository,
            IRepository<Business> businessRepository,
            IRepository<Appointment> appointmentRepository,
            IUnitOfWork unitOfWork)
        {
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
            _businessRepository = businessRepository;
            _appointmentRepository = appointmentRepository;
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

                // 4. Ayni randevu icin daha once yorum yapilmis mi?
                if (request.AppointmentId.HasValue)
                {
                    var existingReviews = await _reviewRepository.FindAsync(
                        r => r.AppointmentId == request.AppointmentId.Value && r.UserId == request.UserId && !r.IsDeleted,
                        cancellationToken);

                    if (existingReviews.Any())
                        return Result<Guid>.Failure(Error.Conflict("Bu randevu icin zaten yorum yapilmis."));
                }

                // 5. Yorum olustur
                var review = new Review(
                    request.UserId,
                    request.BusinessId,
                    request.Rating,
                    request.Comment,
                    request.AppointmentId);

                // 6. Kaydet
                await _reviewRepository.AddAsync(review, cancellationToken);

                // 7. Isletme puanini guncelle
                await UpdateBusinessRating(request.BusinessId, cancellationToken);

                // 8. Degisiklikleri kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(review.Id);
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

        private async Task UpdateBusinessRating(Guid businessId, CancellationToken cancellationToken)
        {
            // 1. Isletmenin tum gorunur yorumlarini getir
            var reviews = await _reviewRepository.FindAsync(
                r => r.BusinessId == businessId && r.IsVisible && !r.IsDeleted,
                cancellationToken);

            if (!reviews.Any())
                return;

            // 2. Ortalama puan hesapla
            var averageRating = reviews.Average(r => r.Rating);
            var totalReviews = reviews.Count();

            // 3. Isletmeyi guncelle
            var business = await _businessRepository.GetByIdAsync(businessId, cancellationToken);
            if (business is not null)
            {
                business.UpdateRating((decimal)averageRating);
                // TotalReviews zaten reviews.Count, ama Business entity'sinde TotalReviews property'si var
                // Bu property'nin guncellenmesi gerekiyorsa, business.IncrementTotalReviews() veya benzeri bir metot kullanilabilir
            }
        }
    }
}