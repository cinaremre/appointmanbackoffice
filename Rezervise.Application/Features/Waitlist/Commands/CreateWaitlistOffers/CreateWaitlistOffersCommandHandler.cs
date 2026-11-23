using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Waitlist.Commands.CreateWaitlistOffers
{
    public class CreateWaitlistOffersCommandHandler : IRequestHandler<CreateWaitlistOffersCommand, Result<List<Guid>>>
    {
        private readonly IRepository<WaitlistEntry> _waitlistEntryRepository;
        private readonly IRepository<WaitlistOffer> _waitlistOfferRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTime;

        public CreateWaitlistOffersCommandHandler(
            IRepository<WaitlistEntry> waitlistEntryRepository,
            IRepository<WaitlistOffer> waitlistOfferRepository,
            IRepository<Business> businessRepository,
            IRepository<Service> serviceRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTime)
        {
            _waitlistEntryRepository = waitlistEntryRepository;
            _waitlistOfferRepository = waitlistOfferRepository;
            _businessRepository = businessRepository;
            _serviceRepository = serviceRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Result<List<Guid>>> Handle(CreateWaitlistOffersCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme ve yetki kontrolü: teklif üretme işletme onay adımıdır.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<List<Guid>>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<List<Guid>>.Failure(Error.Forbidden("Bekleme listesi teklifi üretme yetkiniz yok."));

                // 2. Hizmet kontrolü
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
                if (service is null)
                    return Result<List<Guid>>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                if (service.BusinessId != request.BusinessId)
                    return Result<List<Guid>>.Failure(Error.Validation("Hizmet bu işletmeye ait değil.", $"ServiceId: {request.ServiceId}"));

                // 3. Sınır kontrolü
                if (request.MaxOffers <= 0)
                    return Result<List<Guid>>.Failure(Error.Validation("MaxOffers sıfırdan büyük olmalı."));

                // 4. Uygun Active kayıtlar: aynı işletme + aynı hizmet ve slot
                //    kaydın tarih aralığında. PreferredEmployeeId ilk sürümde
                //    eşleştirmede yok sayılır.
                var entries = await _waitlistEntryRepository.FindAsync(
                    e => e.BusinessId == request.BusinessId &&
                         e.ServiceId == request.ServiceId &&
                         e.Status == EnumWaitlistEntryStatus.Active &&
                         !e.IsDeleted,
                    cancellationToken);

                // 5. Aynı slot için hali hazırda Pending teklifi olan kayıtlar
                //    tekrar teklif üretmez; çift teklif engellenir.
                var pendingOffers = await _waitlistOfferRepository.FindAsync(
                    o => o.BusinessId == request.BusinessId &&
                         o.SlotStart == request.SlotStart &&
                         o.Status == EnumWaitlistOfferStatus.Pending &&
                         !o.IsDeleted,
                    cancellationToken);

                var alreadyOfferedEntryIds = pendingOffers
                    .Select(o => o.WaitlistEntryId)
                    .ToHashSet();

                // 6. Önceliklendirme: önce kayıt olan müşteri önce teklif alır.
                var candidates = entries
                    .Where(e => e.MatchesSlot(request.SlotStart) &&
                                !alreadyOfferedEntryIds.Contains(e.Id))
                    .OrderBy(e => e.CreatedAt)
                    .Take(request.MaxOffers)
                    .ToList();

                // 7. Pending teklifleri üret: ExpiresAt = now + WaitlistPolicy.OfferLifetime.
                var now = _dateTime.UtcNow;
                var expiresAt = now.Add(WaitlistPolicy.OfferLifetime);

                var createdOfferIds = new List<Guid>();
                foreach (var entry in candidates)
                {
                    var offer = new WaitlistOffer(
                        entry.Id,
                        request.BusinessId,
                        request.SlotStart,
                        expiresAt,
                        now);

                    await _waitlistOfferRepository.AddAsync(offer, cancellationToken);
                    createdOfferIds.Add(offer.Id);
                }

                // 8. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<List<Guid>>.Success(createdOfferIds);
            }
            catch (DomainException ex)
            {
                return Result<List<Guid>>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<List<Guid>>.Failure(Error.Internal("Bekleme listesi teklifleri üretilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
