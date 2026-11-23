using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Application.Features.Subscriptions.Commands.AssignSubscription;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Commands.AssignSubscriptionByAdmin
{
    /// <summary>
    /// AssignSubscriptionCommand'i IMediator ile sarmalar. Önceki aktif
    /// abonelikler denetim kaydında oldValues olarak özetlenir.
    /// </summary>
    public class AssignSubscriptionByAdminCommandHandler
        : IRequestHandler<AssignSubscriptionByAdminCommand, Result<Guid>>
    {
        private readonly IMediator _mediator;
        private readonly IRepository<BusinessSubscription> _subscriptionRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _adminAuditSink;
        private readonly IAdminAuditContext _adminAuditContext;

        public AssignSubscriptionByAdminCommandHandler(
            IMediator mediator,
            IRepository<BusinessSubscription> subscriptionRepository,
            ICurrentUserService currentUser,
            IAdminAuditSink adminAuditSink,
            IAdminAuditContext adminAuditContext)
        {
            _mediator = mediator;
            _subscriptionRepository = subscriptionRepository;
            _currentUser = currentUser;
            _adminAuditSink = adminAuditSink;
            _adminAuditContext = adminAuditContext;
        }

        public async Task<Result<Guid>> Handle(
            AssignSubscriptionByAdminCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Denetim için mevcut aktif abonelikleri önceden oku;
                //    atama sonrası bunlar Expired olacak.
                var activeSubscriptions = await _subscriptionRepository.FindAsync(
                    s => s.BusinessId == request.BusinessId
                        && s.Status == EnumSubscriptionStatus.Active
                        && !s.IsDeleted,
                    cancellationToken);

                var oldValues = activeSubscriptions.Count == 0
                    ? "ActiveCount=0"
                    : $"ActiveCount={activeSubscriptions.Count}; "
                        + string.Join(", ", activeSubscriptions.Select(s => $"{s.Id}({s.PlanId})"));

                // 2. Domain komutuna çevir; kurallar içerdeki handler'dadır.
                var command = new AssignSubscriptionCommand
                {
                    BusinessId = request.BusinessId,
                    PlanId = request.PlanId,
                    StartAt = request.StartAt,
                    EndAt = request.EndAt
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (result.IsFailure)
                    return Result<Guid>.Failure(result.Error!);

                // 3. Kritik işlem denetimi: işletme kimliği ile birlikte yazılır.
                await AdminAuditRecorder.RecordAsync(
                    _adminAuditSink,
                    _adminAuditContext,
                    _currentUser.UserId,
                    businessId: request.BusinessId,
                    entityType: "BusinessSubscription",
                    entityId: result.Value.ToString(),
                    action: "AssignSubscription",
                    oldValues: oldValues,
                    newValues: $"PlanId={request.PlanId}; StartAt={request.StartAt:yyyy-MM-dd}; "
                        + $"EndAt={request.EndAt?.ToString("yyyy-MM-dd") ?? "belirsiz"}",
                    cancellationToken: cancellationToken);

                return Result<Guid>.Success(result.Value);
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(
                    Error.Internal("Admin abonelik ataması sırasında bir hata oluştu.", ex.Message));
            }
        }
    }
}
