using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Reviews.Queries.GetMyReviews
{
    /// <summary>
    /// İşletme sahibinin kendi işletmesine yapılan yorumlar. Sahiplik
    /// handler'da doğrulanır; en yeni yorum önce listelenir.
    /// </summary>
    public class GetMyReviewsQuery : IRequest<Result<AdminPagedResult<OwnerReviewDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>1 tabanlı sayfa numarası.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sınırlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner yorum satırı.</summary>
    public class OwnerReviewDto
    {
        public Guid Id { get; init; }

        /// <summary>Yorumu yazan müşteri (User) ID.</summary>
        public Guid CustomerId { get; init; }

        public int Rating { get; init; }

        public string Comment { get; init; } = string.Empty;

        /// <summary>Yorum işletme tarafından gizlenmiş mi?</summary>
        public bool IsHidden { get; init; }

        public DateTime CreatedAt { get; init; }
    }
}
