using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Marketing.Queries.GetAdminWaitlistOverview
{
    /// <summary>
    /// Bekleme listesi genel bakisi (dokuman Modul 15): aktif kayit, teklif durum
    /// ve kurtarilan slot sayilari tek raporda toplanir.
    /// </summary>
    public class GetAdminWaitlistOverviewQuery : IRequest<Result<AdminWaitlistOverviewDto>>
    {
        /// <summary>Yalnizca bu isletmenin sayilari; bos ise platform geneli.</summary>
        public Guid? BusinessId { get; set; }
    }
}
