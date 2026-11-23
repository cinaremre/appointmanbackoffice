using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Stock.Queries.GetAdminStockMovements
{
    public class GetAdminStockMovementsQueryHandler
        : IRequestHandler<GetAdminStockMovementsQuery, Result<AdminPagedResult<AdminStockMovementDto>>>
    {
        private readonly IRepository<StockMovement> _stockMovementRepository;

        public GetAdminStockMovementsQueryHandler(IRepository<StockMovement> stockMovementRepository)
        {
            _stockMovementRepository = stockMovementRepository;
        }

        public async Task<Result<AdminPagedResult<AdminStockMovementDto>>> Handle(
            GetAdminStockMovementsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var page = AdminPagination.ClampPage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var productId = request.ProductId;
                var fromDate = request.FromDate;
                var toDate = request.ToDate;

                var movements = await _stockMovementRepository.FindAsync(
                    m => !m.IsDeleted
                        && (businessId == null || m.BusinessId == businessId)
                        && (productId == null || m.ProductId == productId)
                        && (fromDate == null || m.OccurredAt >= fromDate)
                        && (toDate == null || m.OccurredAt <= toDate),
                    cancellationToken);

                var ordered = movements.OrderByDescending(m => m.OccurredAt).ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new AdminStockMovementDto
                    {
                        Id = m.Id,
                        BusinessId = m.BusinessId,
                        ProductId = m.ProductId,
                        MovementType = m.MovementType,
                        Quantity = m.Quantity,
                        UnitCost = m.UnitCost,
                        ReferenceType = m.ReferenceType,
                        ReferenceId = m.ReferenceId,
                        OccurredAt = m.OccurredAt
                    })
                    .ToList();

                return Result<AdminPagedResult<AdminStockMovementDto>>.Success(
                    new AdminPagedResult<AdminStockMovementDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<AdminStockMovementDto>>.Failure(
                    Error.Internal("Stok hareket listesi getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
