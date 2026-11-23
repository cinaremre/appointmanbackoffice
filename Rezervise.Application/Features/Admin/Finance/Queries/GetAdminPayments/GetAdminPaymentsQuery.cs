using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Finance.Queries.GetAdminPayments
{
    /// <summary>
    /// Ödeme listesi (doküman Modül 16). İadeler ayrı bir ters kayıt olarak
    /// tutulur; IsRefund bayrağı ile normal ödemeler ve iadeler ayrı ayrı
    /// listelenebilir. Salt okunur endpoint.
    /// </summary>
    public class GetAdminPaymentsQuery : IRequest<Result<AdminPagedResult<AdminPaymentDto>>>
    {
        public Guid? BusinessId { get; set; }

        public EnumPaymentMethod? PaymentMethod { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        /// <summary>True = yalnızca iade kayıtları; false = yalnızca normal ödemeler.</summary>
        public bool? IsRefund { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Tek ödeme kaydının admin görünümü. İade kayıtları RefersToPaymentId ile orijinale bağlanır.</summary>
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

        /// <summary>Bu kayıt bir iade (ters) kaydı mi?</summary>
        public bool IsRefund { get; set; }

        /// <summary>Ters kayıt ise bağlı öldüğü orijinal ödeme ID.</summary>
        public Guid? RefersToPaymentId { get; set; }
    }
}
