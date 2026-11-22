using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Owner.Finance.Queries.GetMyPayments
{
    /// <summary>
    /// Isletme sahibinin odeme listesi. Sahiplik handler'da dogrulanir;
    /// iadeler IsRefund bayragiyla ayri listelenebilir.
    /// </summary>
    public class GetMyPaymentsQuery : IRequest<Result<AdminPagedResult<OwnerPaymentDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Yalnizca iade (ters) kayitlari listele.</summary>
        public bool? IsRefund { get; set; }

        /// <summary>Odeme tarihi alt siniri (UTC, dahil).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Odeme tarihi ust siniri (UTC, dahil).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sinirlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner odeme satiri; iade bilgileri dahil.</summary>
    public class OwnerPaymentDto
    {
        public Guid Id { get; init; }

        public Guid SaleId { get; init; }

        public decimal Amount { get; init; }

        public string Currency { get; init; } = string.Empty;

        public EnumPaymentMethod PaymentMethod { get; init; }

        public DateTime PaidAt { get; init; }

        /// <summary>Durum sayisal deger olarak doner (EnumSaleStatus paylasilir).</summary>
        public EnumSaleStatus Status { get; init; }

        /// <summary>Bu kayit bir iade (ters) kaydi mi?</summary>
        public bool IsRefund { get; init; }

        /// <summary>Ters kayit ise bagli oldugu orijinal odeme ID.</summary>
        public Guid? RefersToPaymentId { get; init; }
    }
}
