using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Owner.Appointments.Queries.GetMyAppointments
{
    /// <summary>
    /// İşletme sahibinin kendi işletmesinin randevuları. Sahiplik
    /// handler'da doğrulanır; durum ve tarih aralığı filtreleriyle
    /// sayfalanır.
    /// </summary>
    public class GetMyAppointmentsQuery : IRequest<Result<AdminPagedResult<OwnerAppointmentListItemDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Randevu durumu filtresi.</summary>
        public EnumAppointmentStatus? Status { get; set; }

        /// <summary>Randevu tarihi alt sınırı (UTC, dahil).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Randevu tarihi üst sınırı (UTC, dahil).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>1 tabanlı sayfa numarası.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sınırlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner randevu liste satırı.</summary>
    public class OwnerAppointmentListItemDto
    {
        public Guid Id { get; init; }

        public Guid UserId { get; init; }

        public Guid EmployeeId { get; init; }

        public Guid ServiceId { get; init; }

        public DateTime AppointmentDate { get; init; }

        /// <summary>Durum sayısal değer olarak döner (EnumAppointmentStatus).</summary>
        public EnumAppointmentStatus Status { get; init; }

        public string? Notes { get; init; }
    }
}
