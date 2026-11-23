using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Stock.Queries.GetMyProducts
{
    /// <summary>
    /// İşletme sahibinin ürün listesi. Sahiplik OwnerScope ile doğrulanır;
    /// stok bakiyesi sayfadaki ürünlerin hareketleri tek sorguda çekilip
    /// bellek içinde toplanır (GetAdminProducts deseniyle aynı).
    /// </summary>
    public class GetMyProductsQueryHandler
        : IRequestHandler<GetMyProductsQuery, Result<AdminPagedResult<OwnerProductDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<StockMovement> _stockMovementRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyProductsQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Product> productRepository,
            IRepository<StockMovement> stockMovementRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _productRepository = productRepository;
            _stockMovementRepository = stockMovementRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<AdminPagedResult<OwnerProductDto>>> Handle(
            GetMyProductsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahiplik: işletme JWT sahibine ait olmalı.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<AdminPagedResult<OwnerProductDto>>.Failure(owned.Error!);

                // 2. Sayfalama kuralları.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
                var isActive = request.IsActive;

                // 3. Ürün filtresi: işletme, durum ve serbest metin (ad/SKU).
                var products = await _productRepository.FindAsync(
                    p => p.BusinessId == businessId
                        && !p.IsDeleted
                        && (search == null || p.Name.Contains(search) || p.Sku.Contains(search))
                        && (isActive == null || p.IsActive == isActive),
                    cancellationToken);

                var ordered = products
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .ThenBy(p => p.Sku, StringComparer.Ordinal)
                    .ToList();

                var pageItems = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // 4. Stok bakiyesi: sayfadaki ürünlerin hareketlerini tek sorguda
                //    çekip bellek içinde topla (ilk sürüm).
                var productIds = pageItems.Select(p => p.Id).ToList();

                var movements = productIds.Count == 0
                    ? new List<StockMovement>()
                    : (await _stockMovementRepository.FindAsync(
                        m => productIds.Contains(m.ProductId) && !m.IsDeleted,
                        cancellationToken)).ToList();

                // 5. DTO: hareket defterinin işaretli toplamı bakiyedir.
                var items = pageItems
                    .Select(p => new OwnerProductDto
                    {
                        Id = p.Id,
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

                return Result<AdminPagedResult<OwnerProductDto>>.Success(
                    new AdminPagedResult<OwnerProductDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<OwnerProductDto>>.Failure(
                    Error.Internal("Ürün listesi getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
