using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Subscriptions.Commands.AssignSubscription
{
    public class AssignSubscriptionCommandHandler : IRequestHandler<AssignSubscriptionCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<SubscriptionPlan> _planRepository;
        private readonly IRepository<BusinessSubscription> _subscriptionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IWorkflowAuditSink _auditSink;

        public AssignSubscriptionCommandHandler(
            IRepository<Business> businessRepository,
            IRepository<SubscriptionPlan> planRepository,
            IRepository<BusinessSubscription> subscriptionRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IWorkflowAuditSink auditSink)
        {
            _businessRepository = businessRepository;
            _planRepository = planRepository;
            _subscriptionRepository = subscriptionRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _auditSink = auditSink;
        }

        public async Task<Result<Guid>> Handle(AssignSubscriptionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yetki: abonelik atamasi platform yoneticisi isidir.
                if (!CallerAuthorization.IsAdministrator(_currentUser))
                    return Result<Guid>.Failure(Error.Forbidden("Abonelik atamasi yalniz platform yoneticileri icindir."));

                // 2. Isletme ve plan kontrolu.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken);
                if (plan is null)
                    return Result<Guid>.Failure(Error.NotFound("Plan", request.PlanId.ToString()));

                if (!plan.IsActive)
                    return Result<Guid>.Failure(Error.BusinessRule("Atanmak istenen plan aktif degil."));

                // 3. Varsa eski aktif abonelikleri dusur.
                var activeSubscriptions = await _subscriptionRepository.FindAsync(
                    s => s.BusinessId == request.BusinessId
                        && s.Status == EnumSubscriptionStatus.Active
                        && !s.IsDeleted,
                    cancellationToken);

                foreach (var active in activeSubscriptions)
                {
                    active.MarkExpired();
                    await _subscriptionRepository.UpdateAsync(active, cancellationToken);
                }

                // 4. Yeni abonelik olustur.
                var subscription = new BusinessSubscription(
                    request.BusinessId,
                    request.PlanId,
                    request.StartAt,
                    request.EndAt);

                await _subscriptionRepository.AddAsync(subscription, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 5. Denetim kaydi.
                _auditSink.Record(
                    "Subscription.Assigned",
                    Guid.NewGuid(),
                    request.BusinessId,
                    $"Plan={plan.Name}; Start={request.StartAt:yyyy-MM-dd}; EndAt={request.EndAt?.ToString("yyyy-MM-dd") ?? "belirsiz"}; ExpiredCount={activeSubscriptions.Count}");

                return Result<Guid>.Success(subscription.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Abonelik atanirken bir hata olustu.", ex.Message));
            }
        }
    }
}
