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
        /// Atfedilen randevular henuz satis tutari tasimadigi icin klasik
        /// ROAS (gelir/harcama) hesaplanamiyor; aciklama metni DTO ile doner.
        /// Satis/odeme entegrasyonu sonrasi bu sorgu gercek ROAS uretecek.
        /// </summary>
        private const string RoasDescription =
            "Satis entegrasyonu sonrasi ROAS hesaplanacak; bu surumde yalnizca atfedilen randevu sayilari raporlanir.";

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
                // 1. Kampanya kontrolu
                var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
                if (campaign is null)
                    return Result<CampaignRoiDto>.Failure(Error.NotFound("Kampanya", request.CampaignId.ToString()));

                // 2. Yetki kontrolu: kampanyanin isletmesi uzerinden JWT kimligi esas alinir.
                var business = await _businessRepository.GetByIdAsync(campaign.BusinessId, cancellationToken);
                if (business is null)
                    return Result<CampaignRoiDto>.Failure(Error.NotFound("Isletme", campaign.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<CampaignRoiDto>.Failure(Error.Unauthorized("Bu kampanyanin ROI raporunu goruntuleme yetkiniz yok."));

                // 3. Donemdeki harcamalar
                var spends = await _spendRepository.FindAsync(
                    s => s.CampaignId == campaign.Id && !s.IsDeleted
                         && (!request.FromDate.HasValue || s.SpendDate >= request.FromDate.Value)
                         && (!request.ToDate.HasValue || s.SpendDate <= request.ToDate.Value),
                    cancellationToken);

                // 4. Donemdeki atiflar
                var attributions = await _attributionRepository.FindAsync(
                    a => a.CampaignId == campaign.Id && !a.IsDeleted
                         && (!request.FromDate.HasValue || a.AttributedAt >= request.FromDate.Value)
                         && (!request.ToDate.HasValue || a.AttributedAt <= request.ToDate.Value),
                    cancellationToken);

                // 5. Gerceklesen randevu sayisi: atfedilen randevularin durum kontrolu.
                var appointmentIds = attributions.Select(a => a.AppointmentId).Distinct().ToList();

                var completedCount = 0;
                if (appointmentIds.Count > 0)
                {
                    var appointments = await _appointmentRepository.FindAsync(
                        a => appointmentIds.Contains(a.Id) && !a.IsDeleted,
                        cancellationToken);

                    completedCount = appointments.Count(a => a.Status == EnumAppointmentStatus.Completed);
                }

                // 6. DTO olustur
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
                    Error.Internal("Kampanya ROI raporu getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
