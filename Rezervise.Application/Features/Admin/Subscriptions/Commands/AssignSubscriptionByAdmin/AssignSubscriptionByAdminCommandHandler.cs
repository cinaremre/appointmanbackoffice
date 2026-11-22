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
    /// AssignSubscriptionCommand'i IMediator ile sarmalar. Onceki aktif
    /// abonelikler denetim kaydinda oldValues olarak ozetlenir.
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
                // 1. Denetim icin mevcut aktif abonelikleri onceden oku;
                //    atama sonrasi bunlar Expired olacak.
                var activeSubscriptions = await _subscriptionRepository.FindAsync(
                    s => s.BusinessId == request.BusinessId
                        && s.Status == EnumSubscriptionStatus.Active
                        && !s.IsDeleted,
                    cancellationToken);

                var oldValues = activeSubscriptions.Count == 0
                    ? "ActiveCount=0"
                    : $"ActiveCount={activeSubscriptions.Count}; "
                        + string.Join(", ", activeSubscriptions.Select(s => $"{s.Id}({s.PlanId})"));

                // 2. Domain komutuna cevir; kurallar icerdeki handler'dadir.
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

                // 3. Kritik islem denetimi: isletme kimligi ile birlikte yazilir.
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
                    Error.Internal("Admin abonelik atamasi sirasinda bir hata olustu.", ex.Message));
            }
        }
    }
}
