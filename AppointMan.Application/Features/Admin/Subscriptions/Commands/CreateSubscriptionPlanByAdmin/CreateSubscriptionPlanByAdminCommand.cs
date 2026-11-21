using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Subscriptions.Commands.CreateSubscriptionPlanByAdmin
{
    /// <summary>
    /// Backoffice ucu: yeni abonelik plani olusturur (dokuman Modul 13).
    /// Domain kurallari tek kaynak kalsin diye is, mevcut CreatePlanCommand
    /// uzerinden yurutulur; bu sarmalayici yalnizca istegi cevirir ve
    /// basaridan sonra AdminAuditLogs kaydini yazar.
    /// </summary>
    public class CreateSubscriptionPlanByAdminCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "TRY";
        public Domain.Enums.EnumBillingPeriod BillingPeriod { get; set; }

        /// <summary>Plana dahil edilecek ozellikler.</summary>
        public List<AdminPlanFeatureInputDto> Features { get; set; } = new();
    }

    /// <summary>Plana eklenecek tek ozellik satiri.</summary>
    public class AdminPlanFeatureInputDto
    {
        public Guid FeatureId { get; set; }
        public bool IsEnabled { get; set; }

        /// <summary>Donemsel kota; null = sinirsiz.</summary>
        public int? IncludedQuota { get; set; }
    }
}
