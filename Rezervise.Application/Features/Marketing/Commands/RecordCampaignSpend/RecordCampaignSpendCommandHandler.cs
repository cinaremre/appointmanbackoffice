using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Marketing.Commands.RecordCampaignSpend
{
    public class RecordCampaignSpendCommandHandler : IRequestHandler<RecordCampaignSpendCommand, Result<Guid>>
    {
        private readonly IRepository<CampaignSpend> _spendRepository;
        private readonly IRepository<MarketingCampaign> _campaignRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public RecordCampaignSpendCommandHandler(
            IRepository<CampaignSpend> spendRepository,
            IRepository<MarketingCampaign> campaignRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _spendRepository = spendRepository;
            _campaignRepository = campaignRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(RecordCampaignSpendCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kampanya kontrolü
                var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
                if (campaign is null)
                    return Result<Guid>.Failure(Error.NotFound("Kampanya", request.CampaignId.ToString()));

                // 2. Yetki kontrolü: kampanyanın işletmesi üzerinden JWT kimliği esas alınır.
                var business = await _businessRepository.GetByIdAsync(campaign.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", campaign.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu kampanyaya harcama girme yetkiniz yok."));

                // 3. Harcamayı oluştur.
                //    İlk sürümde aynı kampanya+tarih+tutar tekrar girilebilir;
                //    benzersizlik kontrolü bilinçli olarak eklenmedi.
                var spend = new CampaignSpend(
                    campaign.BusinessId,
                    campaign.Id,
                    request.SpendDate,
                    request.Amount,
                    request.Currency,
                    request.DataSource);

                // 4. Kaydet
                await _spendRepository.AddAsync(spend, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(spend.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Kampanya harcaması kaydedilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
