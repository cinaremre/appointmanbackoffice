using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Finance.Queries.GetAdminCommissions
{
    /// <summary>
    /// Prim tahakkuk listesi (doküman Modül 16). Salt okunur endpoint;
    /// ters çevirme ve ödeme domain komutlarıyla yapılır.
    /// </summary>
    public class GetAdminCommissionsQuery : IRequest<Result<AdminPagedResult<AdminCommissionDto>>>
    {
        public Guid? BusinessId { get; set; }
        public Guid? EmployeeId { get; set; }

        public EnumCommissionAccrualStatus? Status { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Tek prim tahakkukunun admin görünümü.</summary>
    public class AdminCommissionDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid? SaleLineId { get; set; }
        public Guid RuleId { get; set; }

        /// <summary>Primin hesaplandığı tahsil edilen tutar.</summary>
        public decimal BaseAmount { get; set; }

        public decimal CommissionAmount { get; set; }
        public EnumCommissionAccrualStatus Status { get; set; }
        public DateTime CalculatedAt { get; set; }
    }
}
