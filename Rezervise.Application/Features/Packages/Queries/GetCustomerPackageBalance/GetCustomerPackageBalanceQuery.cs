using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Packages.Queries.GetCustomerPackageBalance
{
    /// <summary>
    /// Müşteri paketinin hak bakiyesini getirir (doküman Modül 5):
    /// satın alınan toplam, aktif rezervasyon, tüketilen ve kalan seans sayısı.
    /// </summary>
    public class GetCustomerPackageBalanceQuery : IRequest<Result<CustomerPackageBalanceDto>>
    {
        public Guid CustomerPackageId { get; set; }
    }

    /// <summary>Paket bakiyesi. Kalan hak hareket toplamlarından hesaplanır.</summary>
    public class CustomerPackageBalanceDto
    {
        public Guid CustomerPackageId { get; set; }
        public Guid ServicePackageId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid CustomerId { get; set; }
        public EnumCustomerPackageStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }

        /// <summary>Paket tanımında satın alınan toplam seans sayısı.</summary>
        public int PurchasedTotal { get; set; }

        /// <summary>Aktif (kullanılmamış) rezervasyon sayısı.</summary>
        public int ActiveReservations { get; set; }

        /// <summary>Tüketilen seans sayısı.</summary>
        public int Consumed { get; set; }

        /// <summary>Kalan seans sayısı = PurchasedTotal - ActiveReservations - Consumed.</summary>
        public int Remaining { get; set; }
    }
}
