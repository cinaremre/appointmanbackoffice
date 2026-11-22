using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Subscriptions.Commands.CreatePlan
{
    /// <summary>
    /// Yeni abonelik plani olusturur ve ozellik setini baglar (dokuman Modul 14).
    /// Yalniz platform yoneticileri (Admin/SuperAdmin) cagirabilir.
    /// </summary>
    public class CreatePlanCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "TRY";
        public Domain.Enums.EnumBillingPeriod BillingPeriod { get; set; }

        /// <summary>Plana dahil edilecek ozellikler.</summary>
        public List<CreatePlanFeatureDto> Features { get; set; } = new();
    }

    /// <summary>Plana eklenecek tek ozellik satiri.</summary>
    public class CreatePlanFeatureDto
    {
        public Guid FeatureId { get; set; }
        public bool IsEnabled { get; set; }
        public int? IncludedQuota { get; set; }
    }
}
