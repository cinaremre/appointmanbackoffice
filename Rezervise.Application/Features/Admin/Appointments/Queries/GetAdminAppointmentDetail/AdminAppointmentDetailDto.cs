using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentDetail
{
    /// <summary>Randevu detayı ve kısa geçmiş (doküman Modül 12).</summary>
    public class AdminAppointmentDetailDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid ServiceId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public EnumAppointmentStatus Status { get; set; }
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Durum geçmişi; en yeni kayıt başta, en fazla 20 og.</summary>
        public IReadOnlyList<AdminAppointmentHistoryDto> History { get; set; } = Array.Empty<AdminAppointmentHistoryDto>();
    }

    /// <summary>Geçmiş öğesinin özeti.</summary>
    public class AdminAppointmentHistoryDto
    {
        public Guid Id { get; set; }
        public EnumAppointmentStatus OldStatus { get; set; }
        public EnumAppointmentStatus NewStatus { get; set; }
        public string? ChangeReason { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
