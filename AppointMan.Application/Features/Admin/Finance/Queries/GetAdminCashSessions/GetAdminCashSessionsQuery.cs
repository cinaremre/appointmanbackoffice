using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Admin.Finance.Queries.GetAdminCashSessions
{
    /// <summary>
    /// Kasa oturumu listesi (dokuman Modul 16). Fark (Difference) oturum
    /// kapanirken domain tarafinda hesaplanir; burada salt okunur.
    /// </summary>
    public class GetAdminCashSessionsQuery : IRequest<Result<AdminPagedResult<AdminCashSessionDto>>>
    {
        public Guid? BusinessId { get; set; }

        public EnumCashSessionStatus? Status { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Tek kasa oturumunun admin gorunumu.</summary>
    public class AdminCashSessionDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid CashRegisterId { get; set; }
        public Guid OpenedBy { get; set; }
        public DateTime OpenedAt { get; set; }
        public decimal OpeningCash { get; set; }

        public Guid? ClosedBy { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal? ExpectedCash { get; set; }
        public decimal? CountedCash { get; set; }

        /// <summary>Sayilan - beklenen nakit farki; acik oturumda null.</summary>
        public decimal? Difference { get; set; }

        public EnumCashSessionStatus Status { get; set; }
    }
}
