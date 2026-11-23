using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Owner.Stock.Queries.GetMyPackages
{
    /// <summary>
    /// İşletme sahibinin müşterilerine satılan paket örnekleri. Kalan hak
    /// entity'de tutulmaz; Reserve/Consume hareketlerinden hesaplanır.
    /// </summary>
    public class GetMyPackagesQuery : IRequest<Result<AdminPagedResult<OwnerCustomerPackageDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>1 tabanlı sayfa numarası.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sınırlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner müşteri paketi satırı; kalan hak özetiyle.</summary>
    public class OwnerCustomerPackageDto
    {
        public Guid Id { get; init; }

        public Guid CustomerId { get; init; }

        /// <summary>Satın alınan paket tanımı ID.</summary>
        public Guid PackageId { get; init; }

        public DateTime PurchasedAt { get; init; }

        public DateTime ExpiresAt { get; init; }

        /// <summary>Durum sayısal değer olarak döner (EnumCustomerPackageStatus).</summary>
        public EnumCustomerPackageStatus Status { get; init; }

        /// <summary>Paket tanımındaki toplam seans sayısı.</summary>
        public int Purchased { get; init; }

        /// <summary>Tüketilen (Consume) seans sayısı.</summary>
        public int Consumed { get; init; }

        /// <summary>Rezerve (Reserve) seans sayısı.</summary>
        public int Reserved { get; init; }

        /// <summary>Kalan hak: Purchased - Consumed - Reserved.</summary>
        public int Remaining { get; init; }
    }
}
