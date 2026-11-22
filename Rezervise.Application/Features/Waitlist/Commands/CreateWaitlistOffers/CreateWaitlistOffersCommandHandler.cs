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
                // 1. Isletme ve yetki kontrolu: teklif uretme isletme onay adimidir.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<List<Guid>>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<List<Guid>>.Failure(Error.Forbidden("Bekleme listesi teklifi uretme yetkiniz yok."));

                // 2. Hizmet kontrolu
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
                if (service is null)
                    return Result<List<Guid>>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                if (service.BusinessId != request.BusinessId)
                    return Result<List<Guid>>.Failure(Error.Validation("Hizmet bu isletmeye ait degil.", $"ServiceId: {request.ServiceId}"));

                // 3. Sinir kontrolu
                if (request.MaxOffers <= 0)
                    return Result<List<Guid>>.Failure(Error.Validation("MaxOffers sifirdan buyuk olmali."));

                // 4. Uygun Active kayitlar: ayni isletme + ayni hizmet ve slot
                //    kaydin tarih araliginda. PreferredEmployeeId ilk surumde
                //    eslestirmede yok sayilir.
                var entries = await _waitlistEntryRepository.FindAsync(
                    e => e.BusinessId == request.BusinessId &&
                         e.ServiceId == request.ServiceId &&
                         e.Status == EnumWaitlistEntryStatus.Active &&
                         !e.IsDeleted,
                    cancellationToken);

                // 5. Ayni slot icin hali hazirda Pending teklifi olan kayitlar
                //    tekrar teklif uretmez; cift teklif engellenir.
                var pendingOffers = await _waitlistOfferRepository.FindAsync(
                    o => o.BusinessId == request.BusinessId &&
                         o.SlotStart == request.SlotStart &&
                         o.Status == EnumWaitlistOfferStatus.Pending &&
                         !o.IsDeleted,
                    cancellationToken);

                var alreadyOfferedEntryIds = pendingOffers
                    .Select(o => o.WaitlistEntryId)
                    .ToHashSet();

                // 6. Onceliklendirme: once kayit olan musteri once teklif alir.
                var candidates = entries
                    .Where(e => e.MatchesSlot(request.SlotStart) &&
                                !alreadyOfferedEntryIds.Contains(e.Id))
                    .OrderBy(e => e.CreatedAt)
                    .Take(request.MaxOffers)
                    .ToList();

                // 7. Pending teklifleri uret: ExpiresAt = now + WaitlistPolicy.OfferLifetime.
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
                return Result<List<Guid>>.Failure(Error.Internal("Bekleme listesi teklifleri uretilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
