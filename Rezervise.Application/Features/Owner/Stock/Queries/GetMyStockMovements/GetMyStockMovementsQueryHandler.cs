using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Stock.Queries.GetMyStockMovements
{
    /// <summary>
    /// İşletme sahibinin stok hareket listesi. Sahiplik OwnerScope ile
    /// doğrulanır; defter kayıtları ürün ve tarih filtreleriyle listelenir
    /// (GetAdminStockMovements deseniyle aynı).
    /// </summary>
    public class GetMyStockMovementsQueryHandler
        : IRequestHandler<GetMyStockMovementsQuery, Result<AdminPagedResult<OwnerStockMovementDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<StockMovement> _stockMovementRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyStockMovementsQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<StockMovement> stockMovementRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _stockMovementRepository = stockMovementRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<AdminPagedResult<OwnerStockMovementDto>>> Handle(
            GetMyStockMovementsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahiplik: işletme JWT sahibine ait olmalı.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<AdminPagedResult<OwnerStockMovementDto>>.Failure(owned.Error!);

                // 2. Sayfalama kuralları.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var productId = request.ProductId;
                var fromDate = request.FromDate;
                var toDate = request.ToDate;

                // 3. Filtre: işletme + ürün + tarih aralığı.
                var movements = await _stockMovementRepository.FindAsync(
                    m => m.BusinessId == businessId
                        && !m.IsDeleted
                        && (productId == null || m.ProductId == productId)
                        && (fromDate == null || m.OccurredAt >= fromDate)
                        && (toDate == null || m.OccurredAt <= toDate),
                    cancellationToken);

                // 4. En yeni hareket önce.
                var ordered = movements
                    .OrderByDescending(m => m.OccurredAt)
                    .ThenBy(m => m.Id)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new OwnerStockMovementDto
                    {
                        Id = m.Id,
                        ProductId = m.ProductId,
                        MovementType = m.MovementType,
                        Quantity = m.Quantity,
                        UnitCost = m.UnitCost,
                        ReferenceType = m.ReferenceType,
                        ReferenceId = m.ReferenceId,
                        OccurredAt = m.OccurredAt
                    })
                    .ToList();

                return Result<AdminPagedResult<OwnerStockMovementDto>>.Success(
                    new AdminPagedResult<OwnerStockMovementDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<OwnerStockMovementDto>>.Failure(
                    Error.Internal("Stok hareket listesi getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
