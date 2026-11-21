using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Commissions.Commands.AccrueCommission
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
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu isletmeye prim tahakkuk etme yetkiniz yok."));

                // 3. Calisan kontrolu: calisan isletmeye ait olmali.
                var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
                if (employee is null || employee.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.NotFound("Calisan", request.EmployeeId.ToString()));

                // 4. Uygun kural secimi: su an gecerli (aktif + pencere icinde) ve
                //    kapsam alanlari eslesen kurallar arasindan en spesifik olani.
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
                        "Bu islem icin uygun aktif prim kurali bulunamadi.",
                        $"Kural penceresi disi veya kapsam alani eslesmeyen kural. CalisanId: {request.EmployeeId}"));

                // 5. Hesap: yuzde tiplerinde BaseAmount * Rate, sabit tipte FixedAmount.
                //    BaseAmount cagiranin verdigi tahsil edilen tutardir (tahsilat tabanli).
                var commissionAmount = rule.Calculate(request.BaseAmount);

                // 6. Tahakkuk kaydini olustur.
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
                return Result<Guid>.Failure(Error.Internal("Prim tahakkuk edilirken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>
        /// Kural kapsam alani islemle cakisir mi? Kural bir alana bagliysa o alan
        /// islemde verilmis/eslesen olmali; bagli degilse kural genel kabul edilir.
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
        /// Spesifiklik puani: EmployeeId+ServiceId (veya ProductId) eslesmesi en
        /// spesifik, yalniz EmployeeId eslesmesi ikinci, yalniz ServiceId genel,
        /// kapsam alani olmayan genel kural en dusuk puanlidir.
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
