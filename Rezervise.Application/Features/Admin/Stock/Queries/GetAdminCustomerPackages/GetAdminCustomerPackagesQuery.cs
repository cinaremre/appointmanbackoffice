using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Stock.Queries.GetAdminCustomerPackages
{
    /// <summary>
    /// Müşteri paketi listesi + kalan hak (doküman Modül 17). Kalan hak
    /// entity'de tutulmaz: purchased toplamı paket tanımından, tüketilen ve
    /// rezerve haklar PackageUsage hareketlerinden toplanır. Hesap mantığı
    /// GetCustomerPackageBalance sorgusuyla aynı benim: Remaining =
    /// Purchased - Consumed - Reserved.
    /// </summary>
    public class GetAdminCustomerPackagesQuery : IRequest<Result<AdminPagedResult<AdminCustomerPackageDto>>>
    {
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }

        public EnumCustomerPackageStatus? Status { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Müşteri paketinin kalan hak özetiyle admin görünümü.</summary>
    public class AdminCustomerPackageDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid CustomerId { get; set; }

        /// <summary>Satın alınan paket tanımı.</summary>
        public Guid PackageId { get; set; }

        public DateTime PurchasedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public EnumCustomerPackageStatus Status { get; set; }

        /// <summary>Paket tanımındaki toplam seans sayısı.</summary>
        public int Purchased { get; set; }

        /// <summary>Aktif Consumption hareketi sayısı.</summary>
        public int Consumed { get; set; }

        /// <summary>Aktif Reservation hareketi sayısı.</summary>
        public int Reserved { get; set; }

        /// <summary>Purchased - Consumed - Reserved.</summary>
        public int Remaining { get; set; }
    }
}
