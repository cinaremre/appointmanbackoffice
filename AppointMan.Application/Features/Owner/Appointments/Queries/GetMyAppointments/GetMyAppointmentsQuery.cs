using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Owner.Appointments.Queries.GetMyAppointments
{
    /// <summary>
    /// Isletme sahibinin kendi isletmesinin randevulari. Sahiplik
    /// handler'da dogrulanir; durum ve tarih araligi filtreleriyle
    /// sayfalanir.
    /// </summary>
    public class GetMyAppointmentsQuery : IRequest<Result<AdminPagedResult<OwnerAppointmentListItemDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Randevu durumu filtresi.</summary>
        public EnumAppointmentStatus? Status { get; set; }

        /// <summary>Randevu tarihi alt siniri (UTC, dahil).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Randevu tarihi ust siniri (UTC, dahil).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sinirlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner randevu liste satiri.</summary>
    public class OwnerAppointmentListItemDto
    {
        public Guid Id { get; init; }

        public Guid UserId { get; init; }

        public Guid EmployeeId { get; init; }

        public Guid ServiceId { get; init; }

        public DateTime AppointmentDate { get; init; }

        /// <summary>Durum sayisal deger olarak doner (EnumAppointmentStatus).</summary>
        public EnumAppointmentStatus Status { get; init; }

        public string? Notes { get; init; }
    }
}
