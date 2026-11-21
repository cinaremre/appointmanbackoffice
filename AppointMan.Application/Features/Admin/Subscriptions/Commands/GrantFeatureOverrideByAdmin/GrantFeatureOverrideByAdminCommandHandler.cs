using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Common;
using AppointMan.Application.Features.Subscriptions.Commands.GrantFeatureOverride;
using MediatR;

namespace AppointMan.Application.Features.Admin.Subscriptions.Commands.GrantFeatureOverrideByAdmin
{
    /// <summary>
    /// GrantFeatureOverrideCommand'i IMediator ile sarmalar. Icerdeki
    /// handler admin rol kontrolunu ve IWorkflowAuditSink kaydini yaptigi
    /// icin burada yalnizca AdminAuditLogs kaydi eklenir.
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
                // 1. Domain komutuna cevir; kurallar icerdeki handler'dadir.
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

                // 2. Kritik islem denetimi: isletme + ozellik + gecerlilik ozeti.
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
                    Error.Internal("Admin ozellik override sirasinda bir hata olustu.", ex.Message));
            }
        }
    }
}
