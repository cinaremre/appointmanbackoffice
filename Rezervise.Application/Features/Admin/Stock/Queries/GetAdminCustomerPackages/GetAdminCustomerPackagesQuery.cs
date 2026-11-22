using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Stock.Queries.GetAdminCustomerPackages
{
    /// <summary>
    /// Musteri paketi listesi + kalan hak (dokuman Modul 17). Kalan hak
    /// entity'de tutulmaz: purchased toplami paket tanimindan, tuketilen ve
    /// rezerve haklar PackageUsage hareketlerinden toplanir. Hesap mantigi
    /// GetCustomerPackageBalance sorgusuyla ayni benim: Remaining =
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

    /// <summary>Musteri paketinin kalan hak ozetiyle admin gorunumu.</summary>
    public class AdminCustomerPackageDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid CustomerId { get; set; }

        /// <summary>Satin alinan paket tanimi.</summary>
        public Guid PackageId { get; set; }

        public DateTime PurchasedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public EnumCustomerPackageStatus Status { get; set; }

        /// <summary>Paket tanimindaki toplam seans sayisi.</summary>
        public int Purchased { get; set; }

        /// <summary>Aktif Consumption hareketi sayisi.</summary>
        public int Consumed { get; set; }

        /// <summary>Aktif Reservation hareketi sayisi.</summary>
        public int Reserved { get; set; }

        /// <summary>Purchased - Consumed - Reserved.</summary>
        public int Remaining { get; set; }
    }
}
