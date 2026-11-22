using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Owner.Stock.Queries.GetMyPackages
{
    /// <summary>
    /// Isletme sahibinin musterilerine satilan paket ornekleri. Kalan hak
    /// entity'de tutulmaz; Reserve/Consume hareketlerinden hesaplanir.
    /// </summary>
    public class GetMyPackagesQuery : IRequest<Result<AdminPagedResult<OwnerCustomerPackageDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sinirlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner musteri paketi satiri; kalan hak ozetiyle.</summary>
    public class OwnerCustomerPackageDto
    {
        public Guid Id { get; init; }

        public Guid CustomerId { get; init; }

        /// <summary>Satin alinan paket tanimi ID.</summary>
        public Guid PackageId { get; init; }

        public DateTime PurchasedAt { get; init; }

        public DateTime ExpiresAt { get; init; }

        /// <summary>Durum sayisal deger olarak doner (EnumCustomerPackageStatus).</summary>
        public EnumCustomerPackageStatus Status { get; init; }

        /// <summary>Paket tanimindaki toplam seans sayisi.</summary>
        public int Purchased { get; init; }

        /// <summary>Tuketilen (Consume) seans sayisi.</summary>
        public int Consumed { get; init; }

        /// <summary>Rezerve (Reserve) seans sayisi.</summary>
        public int Reserved { get; init; }

        /// <summary>Kalan hak: Purchased - Consumed - Reserved.</summary>
        public int Remaining { get; init; }
    }
}
