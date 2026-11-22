using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Owner.Finance.Queries.GetMyCashSessions
{
    /// <summary>
    /// Isletme sahibinin kasa oturumu listesi. Sahiplik handler'da
    /// dogrulanir; oturumlar salt okunur listelenir.
    /// </summary>
    public class GetMyCashSessionsQuery : IRequest<Result<AdminPagedResult<OwnerCashSessionDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Oturum durumu filtresi.</summary>
        public EnumCashSessionStatus? Status { get; set; }

        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sinirlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner kasa oturumu satiri; rapo detaylariyla.</summary>
    public class OwnerCashSessionDto
    {
        public Guid Id { get; init; }

        public Guid CashRegisterId { get; init; }

        public Guid OpenedBy { get; init; }

        public DateTime OpenedAt { get; init; }

        public decimal OpeningCash { get; init; }

        public Guid? ClosedBy { get; init; }

        public DateTime? ClosedAt { get; init; }

        public decimal? ExpectedCash { get; init; }

        public decimal? CountedCash { get; init; }

        /// <summary>Beklenen ile sayilan nakit arasindaki fark.</summary>
        public decimal? Difference { get; init; }

        /// <summary>Durum sayisal deger olarak doner (EnumCashSessionStatus).</summary>
        public EnumCashSessionStatus Status { get; init; }
    }
}
