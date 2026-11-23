using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Finance.Queries.GetAdminCashSessions
{
    /// <summary>
    /// Kasa oturumu listesi (doküman Modül 16). Fark (Difference) oturum
    /// kapanırken domain tarafında hesaplanır; burada salt okunur.
    /// </summary>
    public class GetAdminCashSessionsQuery : IRequest<Result<AdminPagedResult<AdminCashSessionDto>>>
    {
        public Guid? BusinessId { get; set; }

        public EnumCashSessionStatus? Status { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Tek kasa oturumunun admin görünümü.</summary>
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

        /// <summary>Sayılan - beklenen nakit farkı; açık oturumda null.</summary>
        public decimal? Difference { get; set; }

        public EnumCashSessionStatus Status { get; set; }
    }
}
