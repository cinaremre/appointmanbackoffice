using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Finance.Queries.GetMySales
{
    /// <summary>
    /// Isletme sahibinin satis listesi. Sahiplik OwnerScope ile
    /// dogrulanir; toplam tutar filtre geneli uzerinden hesaplanir
    /// (GetAdminSales deseniyle ayni).
    /// </summary>
    public class GetMySalesQueryHandler : IRequestHandler<GetMySalesQuery, Result<OwnerSalesListDto>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Sale> _saleRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMySalesQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Sale> saleRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _saleRepository = saleRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<OwnerSalesListDto>> Handle(
            GetMySalesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahiplik: isletme JWT sahibine ait olmali.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<OwnerSalesListDto>.Failure(owned.Error!);

                // 2. Sayfalama kurallari.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var fromDate = request.FromDate;
                var toDate = request.ToDate;

                // 3. Filtre: isletme ve tarih araligi (her ikisi de dahil).
                var sales = await _saleRepository.FindAsync(
                    s => s.BusinessId == businessId
                        && !s.IsDeleted
                        && (fromDate == null || s.SoldAt >= fromDate)
                        && (toDate == null || s.SoldAt <= toDate),
                    cancellationToken);

                // 4. Sirala, sayfala ve toplam tutari filtre geneli uzerinden
                //    hesapla.
                var ordered = sales.OrderByDescending(s => s.SoldAt).ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new OwnerSaleDto
                    {
                        Id = s.Id,
                        SoldAt = s.SoldAt,
                        GrandTotal = s.GrandTotal,
                        Currency = s.Currency,
                        Status = s.Status
                    })
                    .ToList();

                return Result<OwnerSalesListDto>.Success(new OwnerSalesListDto
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = ordered.Count,
                    TotalGrandTotal = ordered.Sum(s => s.GrandTotal)
                });
            }
            catch (Exception ex)
            {
                return Result<OwnerSalesListDto>.Failure(
                    Error.Internal("Satis listesi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
