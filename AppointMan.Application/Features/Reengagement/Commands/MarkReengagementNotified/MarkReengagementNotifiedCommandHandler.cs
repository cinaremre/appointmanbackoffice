using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Reengagement.Commands.MarkReengagementNotified
{
    public class MarkReengagementNotifiedCommandHandler : IRequestHandler<MarkReengagementNotifiedCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<BusinessReengagementRule> _ruleRepository;
        private readonly IRepository<ReengagementAttempt> _attemptRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTime;

        public MarkReengagementNotifiedCommandHandler(
            IRepository<Business> businessRepository,
            IRepository<BusinessReengagementRule> ruleRepository,
            IRepository<ReengagementAttempt> attemptRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTime)
        {
            _businessRepository = businessRepository;
            _ruleRepository = ruleRepository;
            _attemptRepository = attemptRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Result<Guid>> Handle(MarkReengagementNotifiedCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki: bildirim isaretleme isletme sahibi veya platform yoneticisi isidir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu isletme icin geri cagirma bildirimi isaretleme yetkiniz yok."));

                // 3. Kural bu isletmeye ait olmali.
                var rule = await _ruleRepository.GetByIdAsync(request.RuleId, cancellationToken);
                if (rule is null || rule.BusinessId != request.BusinessId || rule.IsDeleted)
                    return Result<Guid>.Failure(Error.NotFound("Geri cagirma kurali", request.RuleId.ToString()));

                // 4. Deneme kaydini olustur ve bildirim gonderildi olarak isaretle.
                var attempt = new ReengagementAttempt(
                    request.BusinessId,
                    request.CustomerId,
                    request.ServiceId,
                    request.RuleId,
                    _dateTime.UtcNow);

                attempt.MarkNotified(request.NotificationId);

                await _attemptRepository.AddAsync(attempt, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(attempt.Id);
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Geri cagirma bildirimi isaretlenirken bir hata olustu.", ex.Message));
            }
        }
    }
}
