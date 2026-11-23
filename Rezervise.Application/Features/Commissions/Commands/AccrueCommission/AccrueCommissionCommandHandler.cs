using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Commissions.Commands.AccrueCommission
{
    public class AccrueCommissionCommandHandler : IRequestHandler<AccrueCommissionCommand, Result<Guid>>
    {
        private readonly IRepository<CommissionRule> _ruleRepository;
        private readonly IRepository<CommissionAccrual> _accrualRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTime;

        public AccrueCommissionCommandHandler(
            IRepository<CommissionRule> ruleRepository,
            IRepository<CommissionAccrual> accrualRepository,
            IRepository<Employee> employeeRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTime)
        {
            _ruleRepository = ruleRepository;
            _accrualRepository = accrualRepository;
            _employeeRepository = employeeRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Result<Guid>> Handle(AccrueCommissionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü: gövde OwnerId'si değil, JWT kimliği esas alınır.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu işletmeye prim tahakkuk etme yetkiniz yok."));

                // 3. Çalışan kontrolü: çalışan işletmeye ait olmalı.
                var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
                if (employee is null || employee.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.NotFound("Çalışan", request.EmployeeId.ToString()));

                // 4. Uygun kural seçimi: su an geçerli (aktif + pencere içinde) ve
                //    kapsam alanları eşleşen kurallar arasından en spesifik olanı.
                var utcNow = _dateTime.UtcNow;

                var rules = await _ruleRepository.FindAsync(
                    r => r.BusinessId == request.BusinessId && r.IsActive && !r.IsDeleted,
                    cancellationToken);

                var candidates = rules
                    .Where(r => r.IsEffectiveAt(utcNow))
                    .Where(r => MatchesScope(r, request))
                    .OrderByDescending(r => SpecificityScore(r, request))
                    .ThenByDescending(r => r.EffectiveFrom)
                    .ToList();

                var rule = candidates.FirstOrDefault();
                if (rule is null)
                    return Result<Guid>.Failure(Error.BusinessRule(
                        "Bu işlem için uygun aktif prim kuralı bulunamadı.",
                        $"Kural penceresi dışı veya kapsam alani eşleşmeyen kural. CalisanId: {request.EmployeeId}"));

                // 5. Hesap: yüzde tiplerinde BaseAmount * Rate, sabit tipte FixedAmount.
                //    BaseAmount çağıranın verdiği tahsil edilen tutardır (tahsilat tabanlı).
                var commissionAmount = rule.Calculate(request.BaseAmount);

                // 6. Tahakkuk kaydını oluştur.
                var accrual = new CommissionAccrual(
                    request.BusinessId,
                    request.EmployeeId,
                    rule.Id,
                    request.BaseAmount,
                    commissionAmount,
                    utcNow,
                    request.SaleLineId);

                // 7. Kaydet
                await _accrualRepository.AddAsync(accrual, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(accrual.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Prim tahakkuk edilirken bir hata oluştu.", ex.Message));
            }
        }

        /// <summary>
        /// Kural kapsam alani işlemle çakışır mi? Kural bir alana bağlıysa o alan
        /// işlemde verilmiş/eşleşen olmalı; bağlı değilse kural genel kabul edilir.
        /// </summary>
        private static bool MatchesScope(CommissionRule rule, AccrueCommissionCommand request)
        {
            if (rule.EmployeeId.HasValue && rule.EmployeeId.Value != request.EmployeeId)
                return false;

            if (rule.ServiceId.HasValue && (!request.ServiceId.HasValue || rule.ServiceId.Value != request.ServiceId.Value))
                return false;

            if (rule.ProductId.HasValue && (!request.ProductId.HasValue || rule.ProductId.Value != request.ProductId.Value))
                return false;

            return true;
        }

        /// <summary>
        /// Spesifiklik puanı: EmployeeId+ServiceId (veya ProductId) eşleşmesi en
        /// spesifik, yalnız EmployeeId eşleşmesi ikinci, yalnız ServiceId genel,
        /// kapsam alani olmayan genel kural en düşük puanlıdır.
        /// </summary>
        private static int SpecificityScore(CommissionRule rule, AccrueCommissionCommand request)
        {
            var score = 0;

            if (rule.EmployeeId.HasValue && rule.EmployeeId.Value == request.EmployeeId)
                score += 4;

            if (rule.ServiceId.HasValue && request.ServiceId.HasValue && rule.ServiceId.Value == request.ServiceId.Value)
                score += 2;

            if (rule.ProductId.HasValue && request.ProductId.HasValue && rule.ProductId.Value == request.ProductId.Value)
                score += 2;

            return score;
        }
    }
}
