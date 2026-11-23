using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Marketing.Queries.GetCampaignRoi
{
    public class GetCampaignRoiQueryHandler : IRequestHandler<GetCampaignRoiQuery, Result<CampaignRoiDto>>
    {
        /// <summary>
        /// Atfedilen randevular henüz satış tutarı taşımadığı için klasik
        /// ROAS (gelir/harcama) hesaplanamıyor; açıklama metni DTO ile döner.
        /// Satış/ödeme entegrasyonu sonrası bu sorgu gerçek ROAS üretecek.
        /// </summary>
        private const string RoasDescription =
            "Satış entegrasyonu sonrası ROAS hesaplanacak; bu sürümde yalnızca atfedilen randevu sayıları raporlanır.";

        private readonly IRepository<MarketingCampaign> _campaignRepository;
        private readonly IRepository<CampaignSpend> _spendRepository;
        private readonly IRepository<AppointmentAttribution> _attributionRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly ICurrentUserService _currentUser;

        public GetCampaignRoiQueryHandler(
            IRepository<MarketingCampaign> campaignRepository,
            IRepository<CampaignSpend> spendRepository,
            IRepository<AppointmentAttribution> attributionRepository,
            IRepository<Appointment> appointmentRepository,
            IRepository<Business> businessRepository,
            ICurrentUserService currentUser)
        {
            _campaignRepository = campaignRepository;
            _spendRepository = spendRepository;
            _attributionRepository = attributionRepository;
            _appointmentRepository = appointmentRepository;
            _businessRepository = businessRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<CampaignRoiDto>> Handle(GetCampaignRoiQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kampanya kontrolü
                var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
                if (campaign is null)
                    return Result<CampaignRoiDto>.Failure(Error.NotFound("Kampanya", request.CampaignId.ToString()));

                // 2. Yetki kontrolü: kampanyanın işletmesi üzerinden JWT kimliği esas alınır.
                var business = await _businessRepository.GetByIdAsync(campaign.BusinessId, cancellationToken);
                if (business is null)
                    return Result<CampaignRoiDto>.Failure(Error.NotFound("İşletme", campaign.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<CampaignRoiDto>.Failure(Error.Unauthorized("Bu kampanyanın ROI raporunu görüntüleme yetkiniz yok."));

                // 3. Dönemdeki harcamalar
                var spends = await _spendRepository.FindAsync(
                    s => s.CampaignId == campaign.Id && !s.IsDeleted
                         && (!request.FromDate.HasValue || s.SpendDate >= request.FromDate.Value)
                         && (!request.ToDate.HasValue || s.SpendDate <= request.ToDate.Value),
                    cancellationToken);

                // 4. Dönemdeki atıflar
                var attributions = await _attributionRepository.FindAsync(
                    a => a.CampaignId == campaign.Id && !a.IsDeleted
                         && (!request.FromDate.HasValue || a.AttributedAt >= request.FromDate.Value)
                         && (!request.ToDate.HasValue || a.AttributedAt <= request.ToDate.Value),
                    cancellationToken);

                // 5. Gerçekleşen randevu sayısı: atfedilen randevuların durum kontrolü.
                var appointmentIds = attributions.Select(a => a.AppointmentId).Distinct().ToList();

                var completedCount = 0;
                if (appointmentIds.Count > 0)
                {
                    var appointments = await _appointmentRepository.FindAsync(
                        a => appointmentIds.Contains(a.Id) && !a.IsDeleted,
                        cancellationToken);

                    completedCount = appointments.Count(a => a.Status == EnumAppointmentStatus.Completed);
                }

                // 6. DTO oluştur
                var dto = new CampaignRoiDto
                {
                    CampaignId = campaign.Id,
                    CampaignName = campaign.Name,
                    TotalSpend = spends.Sum(s => s.Amount),
                    AttributedAppointments = attributions.Count,
                    AttributedCompleted = completedCount,
                    RoasDesc = RoasDescription
                };

                return Result<CampaignRoiDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<CampaignRoiDto>.Failure(
                    Error.Internal("Kampanya ROI raporu getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
