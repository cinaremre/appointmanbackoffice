using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminWaitlistOverview
{
    /// <summary>
    /// Uç kaynaktan (WaitlistEntry, WaitlistOffer, RecoveredSlot) sayıları
    /// toplayıp tek özet DTO üretir. Filtre uygulanmışsa yalnızca ilgili
    /// işletmenin kayıtları sayılır.
    /// </summary>
    public class GetAdminWaitlistOverviewQueryHandler
        : IRequestHandler<GetAdminWaitlistOverviewQuery, Result<AdminWaitlistOverviewDto>>
    {
        private readonly IRepository<WaitlistEntry> _entryRepository;
        private readonly IRepository<WaitlistOffer> _offerRepository;
        private readonly IRepository<RecoveredSlot> _recoveredSlotRepository;

        public GetAdminWaitlistOverviewQueryHandler(
            IRepository<WaitlistEntry> entryRepository,
            IRepository<WaitlistOffer> offerRepository,
            IRepository<RecoveredSlot> recoveredSlotRepository)
        {
            _entryRepository = entryRepository;
            _offerRepository = offerRepository;
            _recoveredSlotRepository = recoveredSlotRepository;
        }

        public async Task<Result<AdminWaitlistOverviewDto>> Handle(
            GetAdminWaitlistOverviewQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var businessId = request.BusinessId;

                // 1. Aktif bekleme listesi kayıtları.
                var activeEntries = await _entryRepository.CountAsync(
                    e => !e.IsDeleted
                        && (businessId == null || e.BusinessId == businessId)
                        && e.Status == EnumWaitlistEntryStatus.Active,
                    cancellationToken);

                // 2. Teklifler durum bazında sayılır.
                var offers = await _offerRepository.FindAsync(
                    o => !o.IsDeleted && (businessId == null || o.BusinessId == businessId),
                    cancellationToken);

                // 3. Kurtarılan slotlar Rebooked/Lost olarak sayılır.
                var slots = await _recoveredSlotRepository.FindAsync(
                    r => !r.IsDeleted && (businessId == null || r.BusinessId == businessId),
                    cancellationToken);

                return Result<AdminWaitlistOverviewDto>.Success(new AdminWaitlistOverviewDto
                {
                    BusinessId = businessId,
                    ActiveEntries = activeEntries,
                    PendingOffers = offers.Count(o => o.Status == EnumWaitlistOfferStatus.Pending),
                    AcceptedOffers = offers.Count(o => o.Status == EnumWaitlistOfferStatus.Accepted),
                    ExpiredOffers = offers.Count(o => o.Status == EnumWaitlistOfferStatus.Expired),
                    RebookedSlots = slots.Count(s => s.Status == EnumRecoveredSlotStatus.Rebooked),
                    LostSlots = slots.Count(s => s.Status == EnumRecoveredSlotStatus.Lost)
                });
            }
            catch (Exception ex)
            {
                return Result<AdminWaitlistOverviewDto>.Failure(
                    Error.Internal("Bekleme listesi özeti getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
