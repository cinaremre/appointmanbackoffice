using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Marketing.Commands.RecordCampaignSpend
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
                // 1. Kampanya kontrolu
                var campaign = await _campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
                if (campaign is null)
                    return Result<Guid>.Failure(Error.NotFound("Kampanya", request.CampaignId.ToString()));

                // 2. Yetki kontrolu: kampanyanin isletmesi uzerinden JWT kimligi esas alinir.
                var business = await _businessRepository.GetByIdAsync(campaign.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", campaign.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu kampanyaya harcama girme yetkiniz yok."));

                // 3. Harcamayi olustur.
                //    Ilk surumde ayni kampanya+tarih+tutar tekrar girilebilir;
                //    benzersizlik kontrolu bilincli olarak eklenmedi.
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
                return Result<Guid>.Failure(Error.Internal("Kampanya harcamasi kaydedilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
