using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Admin.Finance.Queries.GetAdminCommissions
{
    /// <summary>
    /// Prim tahakkuk listesi (dokuman Modul 16). Salt okunur endpoint;
    /// ters cevirme ve odeme domain komutlariyla yapilir.
    /// </summary>
    public class GetAdminCommissionsQuery : IRequest<Result<AdminPagedResult<AdminCommissionDto>>>
    {
        public Guid? BusinessId { get; set; }
        public Guid? EmployeeId { get; set; }

        public EnumCommissionAccrualStatus? Status { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Tek prim tahakkukunun admin gorunumu.</summary>
    public class AdminCommissionDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid? SaleLineId { get; set; }
        public Guid RuleId { get; set; }

        /// <summary>Primin hesaplandigi tahsil edilen tutar.</summary>
        public decimal BaseAmount { get; set; }

        public decimal CommissionAmount { get; set; }
        public EnumCommissionAccrualStatus Status { get; set; }
        public DateTime CalculatedAt { get; set; }
    }
}
