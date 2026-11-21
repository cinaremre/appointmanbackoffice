using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Packages.Queries.GetCustomerPackageBalance
{
    /// <summary>
    /// Musteri paketinin hak bakiyesini getirir (dokuman Modul 5):
    /// satin alinan toplam, aktif rezervasyon, tuketilen ve kalan seans sayisi.
    /// </summary>
    public class GetCustomerPackageBalanceQuery : IRequest<Result<CustomerPackageBalanceDto>>
    {
        public Guid CustomerPackageId { get; set; }
    }

    /// <summary>Paket bakiyesi. Kalan hak hareket toplamlarindan hesaplanir.</summary>
    public class CustomerPackageBalanceDto
    {
        public Guid CustomerPackageId { get; set; }
        public Guid ServicePackageId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid CustomerId { get; set; }
        public EnumCustomerPackageStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }

        /// <summary>Paket taniminda satin alinan toplam seans sayisi.</summary>
        public int PurchasedTotal { get; set; }

        /// <summary>Aktif (kullanilmamis) rezervasyon sayisi.</summary>
        public int ActiveReservations { get; set; }

        /// <summary>Tuketilen seans sayisi.</summary>
        public int Consumed { get; set; }

        /// <summary>Kalan seans sayisi = PurchasedTotal - ActiveReservations - Consumed.</summary>
        public int Remaining { get; set; }
    }
}
