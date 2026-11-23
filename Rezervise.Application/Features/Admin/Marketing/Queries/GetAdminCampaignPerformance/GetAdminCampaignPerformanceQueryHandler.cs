using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminCampaignPerformance
{
    /// <summary>
    /// Kampanya performans özeti. Hesaplama deseni mevcut GetCampaignRoi
    /// handler'ıyla uyumludur: harcama toplamı, atıf sayısı ve atfedilen
    /// randevuların Completed durum kontrolü. ROAS açıklaması da aynı biçimde
    /// döner; satış entegrasyonu sonrası gerçek ROAS hesaplanacaktır.
    /// Yönetici görünümü öldüğü için işletme sahibi yetki kontrolü yapılmaz;
    /// erişim API katmanındaki MarketingRead policy ile sınırlanır.
    /// </summary>
    public class GetAdminCampaignPerformanceQueryHandler
        : IRequestHandler<GetAdminCampaignPerformanceQuery, Result<AdminCampaignPerformanceDto>>
    {
        /// <summary>GetCampaignRoi ile aynı ROAS açıklaması.</summary>
        private const string RoasDescription =
            "Satış entegrasyonu sonrası ROAS hesaplanacak; bu sürümde yalnızca atfedilen randevu sayıları raporlanır.";

        private readonly IRepository<MarketingCampaign> _campaignRepository;
        private readonly IRepository<CampaignSpend> _spendRepository;
        private readonly IRepository<AppointmentAttribution> _attributionRepository;
        private readonly IRepository<Appointment> _appointmentRepository;

        public GetAdminCampaignPerformanceQueryHandler(
            IRepository<MarketingCampaign> campaignRepository,
            IRepository<CampaignSpend> spendRepository,
            IRepository<AppointmentAttribution> attributionRepository,
            IRepository<Appointment> appointmentRepository)
        {
            _campaignRepository = campaignRepository;
            _spendRepository = spendRepository;
            _attributionRepository = attributionRepository;
            _appointmentRepository = appointmentRepository;
        }

        public async Task<Result<AdminCampaignPerformanceDto>> Handle(
            GetAdminCampaignPerformanceQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kampanya kontrolü.
                var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
                if (campaign is null)
                    return Result<AdminCampaignPerformanceDto>.Failure(
                        Error.NotFoundById("Kampanya", request.CampaignId));

                // 2. Toplam harcama.
                var spends = await _spendRepository.FindAsync(
                    s => s.CampaignId == campaign.Id && !s.IsDeleted,
                    cancellationToken);

                // 3. Atfedilen randevular.
                var attributions = await _attributionRepository.FindAsync(
                    a => a.CampaignId == campaign.Id && !a.IsDeleted,
                    cancellationToken);

                // 4. Atfedilen randevulardan Completed durumunda olanlar.
                var appointmentIds = attributions
                    .Select(a => a.AppointmentId)
                    .Distinct()
                    .ToList();

                var completedCount = 0;
                if (appointmentIds.Count > 0)
                {
                    var appointments = await _appointmentRepository.FindAsync(
                        a => appointmentIds.Contains(a.Id) && !a.IsDeleted,
                        cancellationToken);

                    completedCount = appointments.Count(a => a.Status == EnumAppointmentStatus.Completed);
                }

                return Result<AdminCampaignPerformanceDto>.Success(new AdminCampaignPerformanceDto
                {
                    CampaignId = campaign.Id,
                    BusinessId = campaign.BusinessId,
                    CampaignName = campaign.Name,
                    TotalSpend = spends.Sum(s => s.Amount),
                    AttributedAppointments = attributions.Count,
                    AttributedCompleted = completedCount,
                    RoasDesc = RoasDescription
                });
            }
            catch (Exception ex)
            {
                return Result<AdminCampaignPerformanceDto>.Failure(
                    Error.Internal("Kampanya performans raporu getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
