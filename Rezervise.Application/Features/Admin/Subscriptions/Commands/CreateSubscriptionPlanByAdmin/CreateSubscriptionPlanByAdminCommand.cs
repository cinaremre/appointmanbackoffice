using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Subscriptions.Commands.CreateSubscriptionPlanByAdmin
{
    /// <summary>
    /// Backoffice ucu: yeni abonelik planı oluşturur (doküman Modül 13).
    /// Domain kuralları tek kaynak kalsın diye is, mevcut CreatePlanCommand
    /// üzerinden yürütülür; bu sarmalayıcı yalnızca isteği çevirir ve
    /// başarıdan sonra AdminAuditLogs kaydını yazar.
    /// </summary>
    public class CreateSubscriptionPlanByAdminCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "TRY";
        public Domain.Enums.EnumBillingPeriod BillingPeriod { get; set; }

        /// <summary>Plana dahil edilecek özellikler.</summary>
        public List<AdminPlanFeatureInputDto> Features { get; set; } = new();
    }

    /// <summary>Plana eklenecek tek özellik satırı.</summary>
    public class AdminPlanFeatureInputDto
    {
        public Guid FeatureId { get; set; }
        public bool IsEnabled { get; set; }

        /// <summary>Dönemsel kota; null = sınırsız.</summary>
        public int? IncludedQuota { get; set; }
    }
}
