using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Features.Admin.Stock.Queries.GetAdminCustomerPackages;
using Rezervise.Application.Features.Admin.Stock.Queries.GetAdminProducts;
using Rezervise.Application.Features.Admin.Stock.Queries.GetAdminStockMovements;
using Rezervise.BackOffice.Api.Common;
using Rezervise.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Stok ve müşteri paketi okuma uçları (doküman Modül 17).
    /// Stok bakiyesi hareket defterinden toplanır; paket kalan hakları
    /// PackageUsage hareketlerinden hesaplanır. Tüm ücra salt okunurdur,
    /// düzeltmeler domain komutlarıyla yapılır.
    ///
    /// Route notu: "stock" ve "packages" grupları aynı controller'da
    /// toplandığından sınıf seviyesinde taban "api/v1/admin" sabitlenir,
    /// şablonlar action seviyesinde verilir.
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

        /// <summary>Ürün listesi + hareket defterinden hesaplanan stok bakiyesi.</summary>
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
        /// Stok hareket listesi; defter kayıtları öldüğü gibi listelenir.
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
        /// Müşteri paketi listesi + kalan hak özeti; paket hareketlerinden
        /// (Rezervasyon/Tüketim) hesaplanır.
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
