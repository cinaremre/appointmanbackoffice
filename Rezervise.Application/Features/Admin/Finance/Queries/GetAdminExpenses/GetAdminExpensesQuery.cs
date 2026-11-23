using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Finance.Queries.GetAdminExpenses
{
    /// <summary>
    /// Gider listesi (doküman Modül 16). Salt okunur endpoint; gider
    /// düzeltmeleri domain komutlarıyla yapılır.
    /// </summary>
    public class GetAdminExpensesQuery : IRequest<Result<AdminPagedResult<AdminExpenseDto>>>
    {
        public Guid? BusinessId { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Tek gider kaydının admin görünümü.</summary>
    public class AdminExpenseDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; }
        public string? Description { get; set; }
        public EnumPaymentMethod PaymentMethod { get; set; }
        public EnumSaleStatus Status { get; set; }
    }
}
