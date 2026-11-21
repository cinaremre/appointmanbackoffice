using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Owner.Reviews.Queries.GetMyReviews
{
    /// <summary>
    /// Isletme sahibinin kendi isletmesine yapilan yorumlar. Sahiplik
    /// handler'da dogrulanir; en yeni yorum once listelenir.
    /// </summary>
    public class GetMyReviewsQuery : IRequest<Result<AdminPagedResult<OwnerReviewDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sinirlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner yorum satiri.</summary>
    public class OwnerReviewDto
    {
        public Guid Id { get; init; }

        /// <summary>Yorumu yazan musteri (User) ID.</summary>
        public Guid CustomerId { get; init; }

        public int Rating { get; init; }

        public string Comment { get; init; } = string.Empty;

        /// <summary>Yorum isletme tarafindan gizlenmis mi?</summary>
        public bool IsHidden { get; init; }

        public DateTime CreatedAt { get; init; }
    }
}
