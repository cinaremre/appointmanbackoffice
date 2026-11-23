using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Sales.Commands.CreateSale
{
    /// <summary>
    /// Yeni satış oluşturur. İlk sürümde satış tek adımda tamamlanır:
    /// kayıt Completed durumuyla oluşur.
    /// </summary>
    public class CreateSaleCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Satışa bağlanacak (tamamlanmış) randevu; opsiyonel.</summary>
        public Guid? AppointmentId { get; set; }

        /// <summary>Müşteri kullanıcı ID; opsiyonel.</summary>
        public Guid? CustomerId { get; set; }

        /// <summary>Para birimi; boş ise "TRY" kullanılır.</summary>
        public string? Currency { get; set; }

        /// <summary>Sipariş seviyesi indirim; satır indirimlerinden ayrı düşülür.</summary>
        public decimal DiscountTotal { get; set; }

        public List<SaleLineInput> Lines { get; set; } = new();

        public List<PaymentInput> Payments { get; set; } = new();

        public class SaleLineInput
        {
            public EnumSaleItemType ItemType { get; set; }
            public Guid? ServiceId { get; set; }
            public Guid? ProductId { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal TaxAmount { get; set; }
            public Guid EmployeeId { get; set; }
        }

        public class PaymentInput
        {
            public decimal Amount { get; set; }
            public EnumPaymentMethod PaymentMethod { get; set; }
        }
    }
}
