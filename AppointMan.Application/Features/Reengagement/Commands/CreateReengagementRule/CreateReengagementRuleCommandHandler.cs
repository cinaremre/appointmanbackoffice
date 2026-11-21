using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Reengagement.Commands.CreateReengagementRule
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
                // 1. Isletme kontrolu.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki: kural tanimlamak isletme sahibi veya platform yoneticisi isidir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu isletmeye geri cagirma kurali ekleme yetkiniz yok."));

                // 3. Kural bir hizmete bagliysa hizmet bu isletmede olmali.
                if (request.ServiceId.HasValue)
                {
                    var serviceExists = await _serviceRepository.ExistsAsync(
                        s => s.Id == request.ServiceId.Value
                            && s.BusinessId == request.BusinessId
                            && !s.IsDeleted,
                        cancellationToken);

                    if (!serviceExists)
                        return Result<Guid>.Failure(Error.BusinessRule("Kurala baglanacak hizmet bu isletmede bulunamadi."));
                }

                // 4. Kural olustur; alan dogrulamasi entity icinde yapilir.
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
                return Result<Guid>.Failure(Error.Internal("Geri cagirma kurali olusturulurken bir hata olustu.", ex.Message));
            }
        }
    }
}
