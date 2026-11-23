using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminWaitlistOverview
{
    /// <summary>
    /// Bekleme listesi genel bakışı (doküman Modül 15): aktif kayıt, teklif durum
    /// ve kurtarılan slot sayıları tek raporda toplanır.
    /// </summary>
    public class GetAdminWaitlistOverviewQuery : IRequest<Result<AdminWaitlistOverviewDto>>
    {
        /// <summary>Yalnızca bu işletmenin sayıları; boş ise platform geneli.</summary>
        public Guid? BusinessId { get; set; }
    }
}
