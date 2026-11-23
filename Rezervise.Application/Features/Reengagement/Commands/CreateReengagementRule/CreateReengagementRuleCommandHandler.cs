using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Reengagement.Commands.CreateReengagementRule
{
    public class CreateReengagementRuleCommandHandler : IRequestHandler<CreateReengagementRuleCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<BusinessReengagementRule> _ruleRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateReengagementRuleCommandHandler(
            IRepository<Business> businessRepository,
            IRepository<Service> serviceRepository,
            IRepository<BusinessReengagementRule> ruleRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _serviceRepository = serviceRepository;
            _ruleRepository = ruleRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(CreateReengagementRuleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki: kural tanımlamak işletme sahibi veya platform yöneticisi işidir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu işletmeye geri çağırma kuralı ekleme yetkiniz yok."));

                // 3. Kural bir hizmete bağlıysa hizmet bu işletmede olmalı.
                if (request.ServiceId.HasValue)
                {
                    var serviceExists = await _serviceRepository.ExistsAsync(
                        s => s.Id == request.ServiceId.Value
                            && s.BusinessId == request.BusinessId
                            && !s.IsDeleted,
                        cancellationToken);

                    if (!serviceExists)
                        return Result<Guid>.Failure(Error.BusinessRule("Kurala bağlanacak hizmet bu işletmede bulunamadı."));
                }

                // 4. Kural oluştur; alan doğrulaması entity içinde yapılır.
                var rule = new BusinessReengagementRule(
                    request.BusinessId,
                    request.ServiceId,
                    request.TemplateMessage,
                    request.IntervalDays,
                    request.LeadDays,
                    request.MaxMessagesPerCycle,
                    request.CooldownDays,
                    request.IsEnabled);

                await _ruleRepository.AddAsync(rule, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(rule.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Geri çağırma kuralı oluşturulurken bir hata oluştu.", ex.Message));
            }
        }
    }
}
