using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Finance.Queries.GetAdminExpenses
{
    public class GetAdminExpensesQueryHandler
        : IRequestHandler<GetAdminExpensesQuery, Result<AdminPagedResult<AdminExpenseDto>>>
    {
        private readonly IRepository<Expense> _expenseRepository;

        public GetAdminExpensesQueryHandler(IRepository<Expense> expenseRepository)
        {
            _expenseRepository = expenseRepository;
        }

        public async Task<Result<AdminPagedResult<AdminExpenseDto>>> Handle(
            GetAdminExpensesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var page = AdminPagination.ClampPage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var fromDate = request.FromDate;
                var toDate = request.ToDate;

                var expenses = await _expenseRepository.FindAsync(
                    e => !e.IsDeleted
                        && (businessId == null || e.BusinessId == businessId)
                        && (fromDate == null || e.ExpenseDate >= fromDate)
                        && (toDate == null || e.ExpenseDate <= toDate),
                    cancellationToken);

                var ordered = expenses.OrderByDescending(e => e.ExpenseDate).ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new AdminExpenseDto
                    {
                        Id = e.Id,
                        BusinessId = e.BusinessId,
                        CategoryId = e.CategoryId,
                        Amount = e.Amount,
                        Currency = e.Currency,
                        ExpenseDate = e.ExpenseDate,
                        Description = e.Description,
                        PaymentMethod = e.PaymentMethod,
                        Status = e.Status
                    })
                    .ToList();

                return Result<AdminPagedResult<AdminExpenseDto>>.Success(
                    new AdminPagedResult<AdminExpenseDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<AdminExpenseDto>>.Failure(
                    Error.Internal("Gider listesi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
