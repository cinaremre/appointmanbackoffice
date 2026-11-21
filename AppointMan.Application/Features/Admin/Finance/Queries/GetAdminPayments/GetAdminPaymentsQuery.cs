using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Admin.Finance.Queries.GetAdminPayments
{
    /// <summary>
    /// Odeme listesi (dokuman Modul 16). Iadeler ayri bir ters kayit olarak
    /// tutulur; IsRefund bayragi ile normal odemeler ve iadeler ayri ayri
    /// listelenebilir. Salt okunur endpoint.
    /// </summary>
    public class GetAdminPaymentsQuery : IRequest<Result<AdminPagedResult<AdminPaymentDto>>>
    {
        public Guid? BusinessId { get; set; }

        public EnumPaymentMethod? PaymentMethod { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        /// <summary>True = yalnizca iade kayitlari; false = yalnizca normal odemeler.</summary>
        public bool? IsRefund { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Tek odeme kaydinin admin gorunumu. Iade kayitlari RefersToPaymentId ile orijinale baglanir.</summary>
    public class AdminPaymentDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid SaleId { get; set; }
        public Guid? CashSessionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public EnumPaymentMethod PaymentMethod { get; set; }
        public DateTime PaidAt { get; set; }
        public EnumSaleStatus Status { get; set; }

        /// <summary>Bu kayit bir iade (ters) kaydi mi?</summary>
        public bool IsRefund { get; set; }

        /// <summary>Ters kayit ise bagli oldugu orijinal odeme ID.</summary>
        public Guid? RefersToPaymentId { get; set; }
    }
}
