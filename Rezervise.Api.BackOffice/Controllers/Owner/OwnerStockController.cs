using Rezervise.Application.Features.Owner.Stock.Queries.GetMyPackages;
using Rezervise.Application.Features.Owner.Stock.Queries.GetMyProducts;
using Rezervise.Application.Features.Owner.Stock.Queries.GetMyStockMovements;
using Rezervise.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers.Owner
{
    /// <summary>
    /// Isletme sahibinin stok ve musteri paketi okuma uclari. Stok bakiyesi
    /// hareket defterinden toplanir; paket kalan haklari PackageUsage
    /// hareketlerinden hesaplanir. Tum uclar salt okunurdur.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "BusinessOwner")]
    [Route("api/v1/owner")]
    public class OwnerStockController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public OwnerStockController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Urun listesi + hareket defterinden hesaplanan stok bakiyesi.</summary>
        [HttpGet("businesses/{businessId:guid}/stock/products")]
        public async Task<IActionResult> GetMyProducts(
            Guid businessId,
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyProductsQuery
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

        /// <summary>Stok hareket listesi; defter kayitlari oldugu gibi listelenir.</summary>
        [HttpGet("businesses/{businessId:guid}/stock/movements")]
        public async Task<IActionResult> GetMyStockMovements(
            Guid businessId,
            [FromQuery] Guid? productId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyStockMovementsQuery
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
        [HttpGet("businesses/{businessId:guid}/packages")]
        public async Task<IActionResult> GetMyPackages(
            Guid businessId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyPackagesQuery
            {
                BusinessId = businessId,
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
