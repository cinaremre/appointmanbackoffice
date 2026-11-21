using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Sales.Queries.GetDailySalesSummary
{
    public class GetDailySalesSummaryQueryHandler
        : IRequestHandler<GetDailySalesSummaryQuery, Result<DailySalesSummaryDto>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Sale> _saleRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRepository<Expense> _expenseRepository;
        private readonly IRepository<CashSession> _cashSessionRepository;
        private readonly ICurrentUserService _currentUser;

        public GetDailySalesSummaryQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Sale> saleRepository,
            IRepository<Payment> paymentRepository,
            IRepository<Expense> expenseRepository,
            IRepository<CashSession> cashSessionRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _saleRepository = saleRepository;
            _paymentRepository = paymentRepository;
            _expenseRepository = expenseRepository;
            _cashSessionRepository = cashSessionRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<DailySalesSummaryDto>> Handle(
            GetDailySalesSummaryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme ve yetki kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<DailySalesSummaryDto>.Failure(
                        Error.NotFoundById("Isletme", request.BusinessId));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<DailySalesSummaryDto>.Failure(
                        Error.Forbidden("Bu isletmenin gunluk ozetini goruntuleme yetkiniz yok."));

                // 2. UTC gun araligi
                var dayStart = request.Date.Date;
                var dayEnd = dayStart.AddDays(1);

                // 3. Gunun tamamlanan satislari
                var sales = await _saleRepository.FindAsync(
                    s => s.BusinessId == request.BusinessId &&
                         !s.IsDeleted &&
                         s.SoldAt >= dayStart && s.SoldAt < dayEnd &&
                         s.Status == EnumSaleStatus.Completed,
                    cancellationToken);

                // 4. Bu satislara bagli odemeler (net tahsilat icin)
                var saleIds = sales.Select(s => s.Id).ToList();

                var payments = saleIds.Count == 0
                    ? new List<Payment>()
                    : (await _paymentRepository.FindAsync(
                        p => p.BusinessId == request.BusinessId &&
                             !p.IsDeleted &&
                             saleIds.Contains(p.SaleId),
                        cancellationToken)).ToList();

                var cashCollected = Math.Round(
                    payments.Where(p => !p.IsRefund && p.PaymentMethod == EnumPaymentMethod.Cash).Sum(p => p.Amount) -
                    payments.Where(p => p.IsRefund && p.PaymentMethod == EnumPaymentMethod.Cash).Sum(p => p.Amount), 2);

                var cardCollected = Math.Round(
                    payments.Where(p => !p.IsRefund && p.PaymentMethod == EnumPaymentMethod.BankCard).Sum(p => p.Amount) -
                    payments.Where(p => p.IsRefund && p.PaymentMethod == EnumPaymentMethod.BankCard).Sum(p => p.Amount), 2);

                // 5. Gunun giderleri
                var expenses = await _expenseRepository.FindAsync(
                    e => e.BusinessId == request.BusinessId &&
                         !e.IsDeleted &&
                         e.ExpenseDate >= dayStart && e.ExpenseDate < dayEnd,
                    cancellationToken);

                // 6. Bekleyen alacak: GrandTotal - odenen (yalnizca pozitif farklar)
                var paidBySale = payments
                    .Where(p => !p.IsRefund)
                    .GroupBy(p => p.SaleId)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

                var outstanding = 0m;
                foreach (var sale in sales)
                {
                    var paid = paidBySale.TryGetValue(sale.Id, out var amount) ? amount : 0m;
                    if (sale.GrandTotal - paid > 0)
                        outstanding += sale.GrandTotal - paid;
                }

                // 7. Kasa acik mi?
                var isCashOpen = await _cashSessionRepository.ExistsAsync(
                    s => s.BusinessId == request.BusinessId &&
                         s.Status == EnumCashSessionStatus.Open &&
                         !s.IsDeleted,
                    cancellationToken);

                var dto = new DailySalesSummaryDto
                {
                    Date = dayStart,
                    BusinessId = request.BusinessId,
                    SalesCount = sales.Count,
                    TotalSales = Math.Round(sales.Sum(s => s.GrandTotal), 2),
                    CashCollected = cashCollected,
                    CardCollected = cardCollected,
                    TotalExpenses = Math.Round(expenses.Sum(e => e.Amount), 2),
                    OutstandingReceivables = Math.Round(outstanding, 2),
                    IsCashOpen = isCashOpen
                };

                return Result<DailySalesSummaryDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<DailySalesSummaryDto>.Failure(
                    Error.Internal("Gunluk satis ozeti getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
