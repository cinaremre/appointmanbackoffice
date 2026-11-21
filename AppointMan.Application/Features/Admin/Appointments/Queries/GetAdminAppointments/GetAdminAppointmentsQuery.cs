using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Admin.Appointments.Queries.GetAdminAppointments
{
    /// <summary>
    /// Backoffice randevu listesi sorgusu (dokuman Modul 12). Isletme, calisan,
    /// durum ve tarih araligi ile filtrelenir; sayfa degerleri sinirlanir.
    /// </summary>
    public class GetAdminAppointmentsQuery : IRequest<Result<AdminPagedResult<AdminAppointmentListItemDto>>>
    {
        /// <summary>Yalnizca bu isletmenin randevulari.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>Yalnizca bu calisanin randevulari.</summary>
        public Guid? EmployeeId { get; set; }

        /// <summary>Randevu durumu filtresi.</summary>
        public EnumAppointmentStatus? Status { get; set; }

        /// <summary>Randevu tarihi alt siniri (UTC, dahil).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Randevu tarihi ust siniri (UTC, dahil).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler tarafinda en fazla 200'e sinirlanir.</summary>
        public int PageSize { get; set; } = 50;
    }
}
