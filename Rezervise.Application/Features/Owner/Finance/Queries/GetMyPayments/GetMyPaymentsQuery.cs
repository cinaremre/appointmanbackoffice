using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Owner.Finance.Queries.GetMyPayments
{
    /// <summary>
    /// İşletme sahibinin ödeme listesi. Sahiplik handler'da doğrulanır;
    /// iadeler IsRefund bayrağıyla ayrı listelenebilir.
    /// </summary>
    public class GetMyPaymentsQuery : IRequest<Result<AdminPagedResult<OwnerPaymentDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Yalnızca iade (ters) kayıtları listele.</summary>
        public bool? IsRefund { get; set; }

        /// <summary>Ödeme tarihi alt sınırı (UTC, dahil).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Ödeme tarihi üst sınırı (UTC, dahil).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanlı sayfa numarası.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sınırlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner ödeme satırı; iade bilgileri dahil.</summary>
    public class OwnerPaymentDto
    {
        public Guid Id { get; init; }

        public Guid SaleId { get; init; }

        public decimal Amount { get; init; }

        public string Currency { get; init; } = string.Empty;

        public EnumPaymentMethod PaymentMethod { get; init; }

        public DateTime PaidAt { get; init; }

        /// <summary>Durum sayısal değer olarak döner (EnumSaleStatus paylaşılır).</summary>
        public EnumSaleStatus Status { get; init; }

        /// <summary>Bu kayıt bir iade (ters) kaydı mi?</summary>
        public bool IsRefund { get; init; }

        /// <summary>Ters kayıt ise bağlı öldüğü orijinal ödeme ID.</summary>
        public Guid? RefersToPaymentId { get; init; }
    }
}
