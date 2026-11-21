using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Waitlist.Commands.AcceptWaitlistOffer
{
    public class AcceptWaitlistOfferCommandHandler : IRequestHandler<AcceptWaitlistOfferCommand, Result<Guid>>
    {
        private readonly IRepository<WaitlistOffer> _waitlistOfferRepository;
        private readonly IRepository<WaitlistEntry> _waitlistEntryRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTime;

        public AcceptWaitlistOfferCommandHandler(
            IRepository<WaitlistOffer> waitlistOfferRepository,
            IRepository<WaitlistEntry> waitlistEntryRepository,
            IRepository<Appointment> appointmentRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTime)
        {
            _waitlistOfferRepository = waitlistOfferRepository;
            _waitlistEntryRepository = waitlistEntryRepository;
            _appointmentRepository = appointmentRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Result<Guid>> Handle(AcceptWaitlistOfferCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Teklif kontrolu
                var offer = await _waitlistOfferRepository.GetByIdAsync(request.OfferId, cancellationToken);
                if (offer is null)
                    return Result<Guid>.Failure(Error.NotFound("Bekleme listesi teklifi", request.OfferId.ToString()));

                // 2. Musteri kimligi teklifin kaydindan cozulur; govdeye guvenilmez.
                var entry = await _waitlistEntryRepository.GetByIdAsync(offer.WaitlistEntryId, cancellationToken);
                if (entry is null)
                    return Result<Guid>.Failure(Error.NotFound("Bekleme listesi kaydi", offer.WaitlistEntryId.ToString()));

                if (!CallerAuthorization.CanActAsUser(_currentUser, entry.CustomerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu teklifi yalnizca kendisine gonderilen musteri kabul edebilir."));

                // 3. Teklifi kabul et (domain kurali: yalniz Pending ve suresi
                //    henuz dolmamis teklifler kabul edilir).
                var now = _dateTime.UtcNow;
                offer.Accept(now);

                // 4. Atomik yeniden kontrol (dokuman 10.6): kabul aninda slot
                //    hala musait mi? Ayni slot icin aktif randevu varsa Conflict.
                var slotTaken = await _appointmentRepository.FindAsync(
                    a => a.BusinessId == offer.BusinessId &&
                         a.AppointmentDate == offer.SlotStart &&
                         (a.Status == EnumAppointmentStatus.Pending ||
                          a.Status == EnumAppointmentStatus.Confirmed) &&
                         !a.IsDeleted,
                    cancellationToken);

                if (slotTaken.Any())
                    return Result<Guid>.Failure(Error.Conflict("Slot artik musait degil."));

                // 5. Ayni slot icin diger Pending teklifleri gecersizlestir.
                var otherPendingOffers = await _waitlistOfferRepository.FindAsync(
                    o => o.BusinessId == offer.BusinessId &&
                         o.SlotStart == offer.SlotStart &&
                         o.Status == EnumWaitlistOfferStatus.Pending &&
                         o.Id != offer.Id &&
                         !o.IsDeleted,
                    cancellationToken);

                foreach (var other in otherPendingOffers)
                    other.Supersede();

                // 6. Kabul edilen musteriye ait kaydi tamamla.
                entry.Fulfill(now);

                await _waitlistOfferRepository.UpdateAsync(offer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(offer.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Bekleme listesi teklifi kabul edilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
