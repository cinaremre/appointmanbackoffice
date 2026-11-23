using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Application.Features.Subscriptions.Commands.CreatePlan;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Commands.CreateSubscriptionPlanByAdmin
{
    /// <summary>
    /// CreatePlanCommand'i IMediator ile sarmalar. Yetki ve domain
    /// doğrulamaları içerdeki handler'dadır; burada yalnızca istek
    /// çevrilir ve başarı sonrası kritik işlem denetimi
    /// AdminAuditRecorder üzerinden yazılır.
    /// </summary>
    public class CreateSubscriptionPlanByAdminCommandHandler
        : IRequestHandler<CreateSubscriptionPlanByAdminCommand, Result<Guid>>
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _adminAuditSink;
        private readonly IAdminAuditContext _adminAuditContext;

        public CreateSubscriptionPlanByAdminCommandHandler(
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
            CreateSubscriptionPlanByAdminCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Domain komutuna çevir; kurallar içerdeki handler'dadır.
                var command = new CreatePlanCommand
                {
                    Name = request.Name,
                    Price = request.Price,
                    Currency = request.Currency,
                    BillingPeriod = request.BillingPeriod,
                    Features = request.Features
                        .Select(f => new CreatePlanFeatureDto
                        {
                            FeatureId = f.FeatureId,
                            IsEnabled = f.IsEnabled,
                            IncludedQuota = f.IncludedQuota
                        })
                        .ToList()
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (result.IsFailure)
                    return Result<Guid>.Failure(result.Error!);

                // 2. Kritik işlem denetimi: plan oluşturma platform genelidir,
                //    businessId boş bırakılır.
                await AdminAuditRecorder.RecordAsync(
                    _adminAuditSink,
                    _adminAuditContext,
                    _currentUser.UserId,
                    businessId: null,
                    entityType: "SubscriptionPlan",
                    entityId: result.Value.ToString(),
                    action: "CreatePlan",
                    newValues: $"Name={request.Name}; Price={request.Price} {request.Currency}; "
                        + $"BillingPeriod={request.BillingPeriod}; FeatureCount={request.Features.Count}",
                    cancellationToken: cancellationToken);

                return Result<Guid>.Success(result.Value);
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(
                    Error.Internal("Admin plan oluşturma sırasında bir hata oluştu.", ex.Message));
            }
        }
    }
}
