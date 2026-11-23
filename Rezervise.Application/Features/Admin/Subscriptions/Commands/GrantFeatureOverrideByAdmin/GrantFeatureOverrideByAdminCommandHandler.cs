using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Application.Features.Subscriptions.Commands.GrantFeatureOverride;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Commands.GrantFeatureOverrideByAdmin
{
    /// <summary>
    /// GrantFeatureOverrideCommand'i IMediator ile sarmalar. İçerdeki
    /// handler admin rol kontrolünü ve IWorkflowAuditSink kaydını yaptığı
    /// için burada yalnızca AdminAuditLogs kaydı eklenir.
    /// </summary>
    public class GrantFeatureOverrideByAdminCommandHandler
        : IRequestHandler<GrantFeatureOverrideByAdminCommand, Result<Guid>>
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _adminAuditSink;
        private readonly IAdminAuditContext _adminAuditContext;

        public GrantFeatureOverrideByAdminCommandHandler(
            IMediator mediator,
            ICurrentUserService currentUser,
            IAdminAuditSink adminAuditSink,
            IAdminAuditContext adminAuditContext)
        {
            _mediator = mediator;
            _currentUser = currentUser;
            _adminAuditSink = adminAuditSink;
            _adminAuditContext = adminAuditContext;
        }

        public async Task<Result<Guid>> Handle(
            GrantFeatureOverrideByAdminCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Domain komutuna çevir; kurallar içerdeki handler'dadır.
                var command = new GrantFeatureOverrideCommand
                {
                    BusinessId = request.BusinessId,
                    FeatureId = request.FeatureId,
                    IsEnabled = request.IsEnabled,
                    QuotaOverride = request.QuotaOverride,
                    ValidFrom = request.ValidFrom,
                    ValidUntil = request.ValidUntil,
                    Reason = request.Reason
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (result.IsFailure)
                    return Result<Guid>.Failure(result.Error!);

                // 2. Kritik işlem denetimi: işletme + özellik + geçerlilik özeti.
                await AdminAuditRecorder.RecordAsync(
                    _adminAuditSink,
                    _adminAuditContext,
                    _currentUser.UserId,
                    businessId: request.BusinessId,
                    entityType: "BusinessFeatureOverride",
                    entityId: result.Value.ToString(),
                    action: "FeatureOverride",
                    newValues: $"FeatureId={request.FeatureId}; IsEnabled={request.IsEnabled}; "
                        + $"QuotaOverride={request.QuotaOverride?.ToString() ?? "plan"}; "
                        + $"ValidFrom={request.ValidFrom:yyyy-MM-dd}; "
                        + $"ValidUntil={request.ValidUntil?.ToString("yyyy-MM-dd") ?? "belirsiz"}",
                    reason: request.Reason,
                    cancellationToken: cancellationToken);

                return Result<Guid>.Success(result.Value);
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(
                    Error.Internal("Admin özellik override sırasında bir hata oluştu.", ex.Message));
            }
        }
    }
}
