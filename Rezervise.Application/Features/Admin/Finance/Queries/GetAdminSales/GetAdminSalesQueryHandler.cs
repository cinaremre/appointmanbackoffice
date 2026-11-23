using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Finance.Queries.GetAdminSales
{
    public class GetAdminSalesQueryHandler : IRequestHandler<GetAdminSalesQuery, Result<AdminSalesListDto>>
    {
        private readonly IRepository<Sale> _saleRepository;

        public GetAdminSalesQueryHandler(IRepository<Sale> saleRepository)
        {
            _saleRepository = saleRepository;
        }

        public async Task<Result<AdminSalesListDto>> Handle(
            GetAdminSalesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sayfalama kuralları: varsayılan 50, üst sınır 200.
                var page = AdminPagination.ClampPage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var fromDate = request.FromDate;
                var toDate = request.ToDate;

                // 2. Filtre: işletme ve tarih aralığı (her ikisi de dahil).
                var sales = await _saleRepository.FindAsync(
                    s => !s.IsDeleted
                        && (businessId == null || s.BusinessId == businessId)
                        && (fromDate == null || s.SoldAt >= fromDate)
                        && (toDate == null || s.SoldAt <= toDate),
                    cancellationToken);

                // 3. Sırala, sayfala ve toplam tutarı filtre geneli üzerinden hesapla.
                var ordered = sales.OrderByDescending(s => s.SoldAt).ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new AdminSaleDto
                    {
                        Id = s.Id,
                        BusinessId = s.BusinessId,
                        CustomerId = s.CustomerId,
                        SoldAt = s.SoldAt,
                        Subtotal = s.Subtotal,
                        DiscountTotal = s.DiscountTotal,
                        TaxTotal = s.TaxTotal,
                        GrandTotal = s.GrandTotal,
                        Currency = s.Currency,
                        Status = s.Status
                    })
                    .ToList();

                return Result<AdminSalesListDto>.Success(new AdminSalesListDto
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
                return Result<AdminSalesListDto>.Failure(
                    Error.Internal("Satış listesi getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
