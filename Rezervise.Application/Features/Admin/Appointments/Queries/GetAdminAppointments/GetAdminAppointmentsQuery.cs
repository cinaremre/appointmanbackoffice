using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Appointments.Queries.GetAdminAppointments
{
    /// <summary>
    /// Backoffice randevu listesi sorgusu (doküman Modül 12). İşletme, çalışan,
    /// durum ve tarih aralığı ile filtrelenir; sayfa değerleri sınırlanır.
    /// </summary>
    public class GetAdminAppointmentsQuery : IRequest<Result<AdminPagedResult<AdminAppointmentListItemDto>>>
    {
        /// <summary>Yalnızca bu işletmenin randevuları.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>Yalnızca bu çalışanın randevuları.</summary>
        public Guid? EmployeeId { get; set; }

        /// <summary>Randevu durumu filtresi.</summary>
        public EnumAppointmentStatus? Status { get; set; }

        /// <summary>Randevu tarihi alt sınırı (UTC, dahil).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Randevu tarihi üst sınırı (UTC, dahil).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanlı sayfa numarası.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler tarafında en fazla 200'e sınırlanır.</summary>
        public int PageSize { get; set; } = 50;
    }
}
