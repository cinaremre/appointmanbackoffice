using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Sales.Commands.CreateSale
{
    /// <summary>
    /// Yeni satis olusturur. Ilk surumde satis tek adimda tamamlanir:
    /// kayit Completed durumuyla olusur.
    /// </summary>
    public class CreateSaleCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Satisa baglanacak (tamamlanmis) randevu; opsiyonel.</summary>
        public Guid? AppointmentId { get; set; }

        /// <summary>Musteri kullanici ID; opsiyonel.</summary>
        public Guid? CustomerId { get; set; }

        /// <summary>Para birimi; bos ise "TRY" kullanilir.</summary>
        public string? Currency { get; set; }

        /// <summary>Siparis seviyesi indirim; satir indirimlerinden ayri dusulur.</summary>
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
