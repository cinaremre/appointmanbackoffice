using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Marketing.Queries.GetAdminCampaigns
{
    /// <summary>
    /// Kampanyalari filtreler ve sayfalara ayirir. Toplam harcama ve atif sayilari
    /// yalnizca gecerli sayfadaki kampanyalar icin hesaplanir; boylece buyuk
    /// kampanya havuzunda gereksiz join maliyeti olusmaz.
    /// </summary>
    public class GetAdminCampaignsQueryHandler
        : IRequestHandler<GetAdminCampaignsQuery, Result<AdminCampaignListDto>>
    {
        private readonly IRepository<MarketingCampaign> _campaignRepository;
        private readonly IRepository<CampaignSpend> _spendRepository;
        private readonly IRepository<AppointmentAttribution> _attributionRepository;

        public GetAdminCampaignsQueryHandler(
            IRepository<MarketingCampaign> campaignRepository,
            IRepository<CampaignSpend> spendRepository,
            IRepository<AppointmentAttribution> attributionRepository)
        {
            _campaignRepository = campaignRepository;
            _spendRepository = spendRepository;
            _attributionRepository = attributionRepository;
        }

        public async Task<Result<AdminCampaignListDto>> Handle(
            GetAdminCampaignsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var businessId = request.BusinessId;
                var channel = request.Channel;
                var isActive = request.IsActive;

                var campaigns = await _campaignRepository.FindAsync(
                    c => !c.IsDeleted
                        && (businessId == null || c.BusinessId == businessId)
                        && (channel == null || c.Channel == channel)
                        && (isActive == null || c.IsActive == isActive),
                    cancellationToken);

                var page = AdminPagination.ClampPage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                var ordered = campaigns
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList();

                var paged = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Sayfadaki kampanyalar icin harcama ve atif toplamlari tek sorguda toplanir.
                var campaignIds = paged.Select(c => c.Id).ToList();

                var spendByCampaign = (await _spendRepository.FindAsync(
                        s => !s.IsDeleted && campaignIds.Contains(s.CampaignId),
                        cancellationToken))
                    .GroupBy(s => s.CampaignId)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.Amount));

                var attributionCounts = (await _attributionRepository.FindAsync(
                        a => !a.IsDeleted && a.CampaignId.HasValue && campaignIds.Contains(a.CampaignId.Value),
                        cancellationToken))
                    .GroupBy(a => a.CampaignId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var items = paged
                    .Select(c => new AdminCampaignDto
                    {
                        Id = c.Id,
                        BusinessId = c.BusinessId,
                        Name = c.Name,
                        Channel = c.Channel,
                        ExternalCampaignId = c.ExternalCampaignId,
                        StartAt = c.StartAt,
                        EndAt = c.EndAt,
                        IsActive = c.IsActive,
                        TotalSpend = spendByCampaign.TryGetValue(c.Id, out var totalSpend) ? totalSpend : 0m,
                        AttributedAppointments = attributionCounts.TryGetValue(c.Id, out var attributed)
                            ? attributed
                            : 0
                    })
                    .ToList();

                return Result<AdminCampaignListDto>.Success(new AdminCampaignListDto
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = ordered.Count
                });
            }
            catch (Exception ex)
            {
                return Result<AdminCampaignListDto>.Failure(
                    Error.Internal("Kampanya listesi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
