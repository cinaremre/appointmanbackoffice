using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Features.Admin.Stock.Queries.GetAdminCustomerPackages;
using AppointMan.Application.Features.Admin.Stock.Queries.GetAdminProducts;
using AppointMan.Application.Features.Admin.Stock.Queries.GetAdminStockMovements;
using AppointMan.BackOffice.Api.Common;
using AppointMan.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.BackOffice.Api.Controllers
{
    /// <summary>
    /// Stok ve musteri paketi okuma uclari (dokuman Modul 17).
    /// Stok bakiyesi hareket defterinden toplanir; paket kalan haklari
    /// PackageUsage hareketlerinden hesaplanir. Tum ucra salt okunurdur,
    /// duzeltmeler domain komutlariyla yapilir.
    ///
    /// Route notu: "stock" ve "packages" gruplari ayni controller'da
    /// toplandigindan sinif seviyesinde taban "api/v1/admin" sabitlenir,
    /// sablonlar action seviyesinde verilir.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/admin")]
    public class AdminStockController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminStockController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Urun listesi + hareket defterinden hesaplanan stok bakiyesi.</summary>
        [HttpGet("stock/products")]
        [Authorize(Policy = AdminPermissions.StockRead)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] Guid? businessId,
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAdminProductsQuery
            {
                BusinessId = businessId,
                Search = search,
                IsActive = isActive,
                Page = page ?? 1,
                PageSize = pageSize ?? 50
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkList(result.Value!.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>
        /// Stok hareket listesi; defter kayitlari oldugu gibi listelenir.
        /// </summary>
        [HttpGet("stock/movements")]
        [Authorize(Policy = AdminPermissions.StockRead)]
        public async Task<IActionResult> GetStockMovements(
            [FromQuery] Guid? businessId,
            [FromQuery] Guid? productId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAdminStockMovementsQuery
            {
                BusinessId = businessId,
                ProductId = productId,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page ?? 1,
                PageSize = pageSize ?? 50
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkList(result.Value!.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>
        /// Musteri paketi listesi + kalan hak ozeti; paket hareketlerinden
        /// (Rezervasyon/Tuketim) hesaplanir.
        /// </summary>
        [HttpGet("packages")]
        [Authorize(Policy = AdminPermissions.PackageRead)]
        public async Task<IActionResult> GetCustomerPackages(
            [FromQuery] Guid? businessId,
            [FromQuery] Guid? customerId,
            [FromQuery] EnumCustomerPackageStatus? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAdminCustomerPackagesQuery
            {
                BusinessId = businessId,
                CustomerId = customerId,
                Status = status,
                Page = page ?? 1,
                PageSize = pageSize ?? 50
            }, cancellationToken);

            var failure = AdminFailureResult.FailureOrNull(result);
            if (failure is not null)
                return failure;

            return OkList(result.Value!.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }
    }
}
