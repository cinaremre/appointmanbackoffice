using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Stock.Queries.GetAdminProducts
{
    public class GetAdminProductsQueryHandler
        : IRequestHandler<GetAdminProductsQuery, Result<AdminPagedResult<AdminProductDto>>>
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<StockMovement> _stockMovementRepository;

        public GetAdminProductsQueryHandler(
            IRepository<Product> productRepository,
            IRepository<StockMovement> stockMovementRepository)
        {
            _productRepository = productRepository;
            _stockMovementRepository = stockMovementRepository;
        }

        public async Task<Result<AdminPagedResult<AdminProductDto>>> Handle(
            GetAdminProductsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var page = AdminPagination.ClampPage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
                var isActive = request.IsActive;

                // 1. Urun filtresi: isletme, durum ve serbest metin (ad/SKU).
                var products = await _productRepository.FindAsync(
                    p => !p.IsDeleted
                        && (businessId == null || p.BusinessId == businessId)
                        && (isActive == null || p.IsActive == isActive)
                        && (search == null || p.Name.Contains(search) || p.Sku.Contains(search)),
                    cancellationToken);

                var ordered = products
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .ThenBy(p => p.Sku, StringComparer.Ordinal)
                    .ToList();

                var pageItems = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // 2. Stok bakiyesi: sayfadaki urunlerin hareketlerini tek sorguda
                //    cekip bellek icinde topla (ilk surum, dokuman Modul 17).
                var productIds = pageItems.Select(p => p.Id).ToList();

                var movements = productIds.Count == 0
                    ? new List<StockMovement>()
                    : (await _stockMovementRepository.FindAsync(
                        m => productIds.Contains(m.ProductId) && !m.IsDeleted,
                        cancellationToken)).ToList();

                // 3. DTO: hareket defterinin isaretli toplami bakiyedir.
                var items = pageItems
                    .Select(p => new AdminProductDto
                    {
                        Id = p.Id,
                        BusinessId = p.BusinessId,
                        Sku = p.Sku,
                        Barcode = p.Barcode,
                        Name = p.Name,
                        Unit = p.Unit,
                        PurchasePrice = p.PurchasePrice,
                        SalePrice = p.SalePrice,
                        MinimumStock = p.MinimumStock,
                        IsActive = p.IsActive,
                        StockBalance = movements
                            .Where(m => m.ProductId == p.Id)
                            .Sum(m => m.Quantity)
                    })
                    .ToList();

                return Result<AdminPagedResult<AdminProductDto>>.Success(
                    new AdminPagedResult<AdminProductDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<AdminProductDto>>.Failure(
                    Error.Internal("Urun listesi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
