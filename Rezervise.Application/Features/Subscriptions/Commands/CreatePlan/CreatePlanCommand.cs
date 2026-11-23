using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Subscriptions.Commands.CreatePlan
{
    /// <summary>
    /// Yeni abonelik planı oluşturur ve özellik setini bağlar (doküman Modül 14).
    /// Yalnız platform yöneticileri (Admin/SuperAdmin) çağırabilir.
    /// </summary>
    public class CreatePlanCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "TRY";
        public Domain.Enums.EnumBillingPeriod BillingPeriod { get; set; }

        /// <summary>Plana dahil edilecek özellikler.</summary>
        public List<CreatePlanFeatureDto> Features { get; set; } = new();
    }

    /// <summary>Plana eklenecek tek özellik satırı.</summary>
    public class CreatePlanFeatureDto
    {
        public Guid FeatureId { get; set; }
        public bool IsEnabled { get; set; }
        public int? IncludedQuota { get; set; }
    }
}
