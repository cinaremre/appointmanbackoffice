using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Admin.Finance.Queries.GetAdminSales
{
    /// <summary>
    /// Satis listesi (dokuman Modul 16). Salt okunur endpoint: tutar
    /// duzeltmeleri iade/ters kayit domain komutlariyla yapilir, burada
    /// veri asla degistirilmez.
    /// </summary>
    public class GetAdminSalesQuery : IRequest<Result<AdminSalesListDto>>
    {
        public Guid? BusinessId { get; set; }

        /// <summary>Dahil; bu tarihten sonraki satislar.</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Dahil; bu tarihe kadar olan satislar.</summary>
        public DateTime? ToDate { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Sayfalanmis satis listesi + filtre kapsamindaki GrandTotal toplami.</summary>
    public class AdminSalesListDto : AdminPagedResult<AdminSaleDto>
    {
        /// <summary>Filtreye uyan TUM satislarin (yalnizca sayfa degil) toplam tutari.</summary>
        public decimal TotalGrandTotal { get; init; }
    }

    /// <summary>Tek satis satirinin admin gorunumu.</summary>
    public class AdminSaleDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime SoldAt { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public string Currency { get; set; } = string.Empty;
        public EnumSaleStatus Status { get; set; }
    }
}
