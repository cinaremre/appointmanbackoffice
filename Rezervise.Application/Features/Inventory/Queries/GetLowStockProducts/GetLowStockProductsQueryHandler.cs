using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Inventory.Queries.GetLowStockProducts
{
    public class GetLowStockProductsQueryHandler : IRequestHandler<GetLowStockProductsQuery, Result<List<LowStockProductDto>>>
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<StockMovement> _stockMovementRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly ICurrentUserService _currentUser;

        public GetLowStockProductsQueryHandler(
            IRepository<Product> productRepository,
            IRepository<StockMovement> stockMovementRepository,
            IRepository<Business> businessRepository,
            ICurrentUserService currentUser)
        {
            _productRepository = productRepository;
            _stockMovementRepository = stockMovementRepository;
            _businessRepository = businessRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<List<LowStockProductDto>>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<List<LowStockProductDto>>.Failure(
                        Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: stok raporunu yalnizca isletme sahibi/yonetici gorur.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<List<LowStockProductDto>>.Failure(
                        Error.Forbidden("Bu isletmenin stok raporunu gorme yetkiniz yok."));

                // 3. Aktif urunleri ve isletmenin tum hareketlerini getir.
                var products = await _productRepository.FindAsync(
                    p => p.BusinessId == request.BusinessId && p.IsActive && !p.IsDeleted,
                    cancellationToken);

                var movements = await _stockMovementRepository.FindAsync(
                    m => m.BusinessId == request.BusinessId && !m.IsDeleted,
                    cancellationToken);

                // 4. Bakiye = urunun hareketlerinin toplami (isaretli miktarlar).
                var balances = movements
                    .GroupBy(m => m.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(m => m.Quantity));

                // 5. Bakiyesi kritik seviyenin altinda olanlari DTO'ya donustur.
                var dtos = products
                    .Select(p => new { Product = p, Balance = balances.GetValueOrDefault(p.Id, 0m) })
                    .Where(x => x.Balance < x.Product.MinimumStock)
                    .OrderBy(x => x.Balance)
                    .ThenBy(x => x.Product.Name)
                    .Select(x => new LowStockProductDto
                    {
                        Id = x.Product.Id,
                        BusinessId = x.Product.BusinessId,
                        Sku = x.Product.Sku,
                        Name = x.Product.Name,
                        Unit = x.Product.Unit,
                        Balance = x.Balance,
                        MinimumStock = x.Product.MinimumStock,
                        Shortage = x.Product.MinimumStock - x.Balance
                    })
                    .ToList();

                return Result<List<LowStockProductDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<LowStockProductDto>>.Failure(
                    Error.Internal("Kritik stok raporu getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
