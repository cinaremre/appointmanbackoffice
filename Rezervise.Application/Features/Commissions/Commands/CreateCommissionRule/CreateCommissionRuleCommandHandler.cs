using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Commissions.Commands.CreateCommissionRule
{
    public class CreateCommissionRuleCommandHandler : IRequestHandler<CreateCommissionRuleCommand, Result<Guid>>
    {
        private readonly IRepository<CommissionRule> _ruleRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateCommissionRuleCommandHandler(
            IRepository<CommissionRule> ruleRepository,
            IRepository<Employee> employeeRepository,
            IRepository<Service> serviceRepository,
            IRepository<Product> productRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _ruleRepository = ruleRepository;
            _employeeRepository = employeeRepository;
            _serviceRepository = serviceRepository;
            _productRepository = productRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(CreateCommissionRuleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu isletmeye prim kurali tanimlama yetkiniz yok."));

                // 3. Kapsam referanslarini dogrula: hepsi ayni isletmeye ait olmali.
                if (request.EmployeeId.HasValue)
                {
                    var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId.Value, cancellationToken);
                    if (employee is null || employee.BusinessId != request.BusinessId)
                        return Result<Guid>.Failure(Error.NotFound("Calisan", request.EmployeeId.Value.ToString()));
                }

                if (request.ServiceId.HasValue)
                {
                    var service = await _serviceRepository.GetByIdAsync(request.ServiceId.Value, cancellationToken);
                    if (service is null || service.BusinessId != request.BusinessId)
                        return Result<Guid>.Failure(Error.NotFound("Hizmet", request.ServiceId.Value.ToString()));
                }

                if (request.ProductId.HasValue)
                {
                    var product = await _productRepository.GetByIdAsync(request.ProductId.Value, cancellationToken);
                    if (product is null || product.BusinessId != request.BusinessId)
                        return Result<Guid>.Failure(Error.NotFound("Urun", request.ProductId.Value.ToString()));
                }

                // 4. Kurali olustur (entity pencere ve oran kurallarini uygular:
                //    EffectiveFrom <= EffectiveTo, oran 0-1 arasi, ...)
                var rule = new CommissionRule(
                    request.BusinessId,
                    request.CalculationType,
                    request.Rate,
                    request.FixedAmount,
                    request.EffectiveFrom,
                    request.EffectiveTo,
                    request.EmployeeId,
                    request.ServiceId,
                    request.ProductId);

                // 5. Kaydet
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
                return Result<Guid>.Failure(Error.Internal("Prim kurali olusturulurken bir hata olustu.", ex.Message));
            }
        }
    }
}
