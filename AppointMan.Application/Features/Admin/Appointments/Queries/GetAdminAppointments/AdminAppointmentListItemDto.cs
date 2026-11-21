using AppointMan.Domain.Enums;

namespace AppointMan.Application.Features.Admin.Appointments.Queries.GetAdminAppointments
{
    /// <summary>Randevu listesi ogesi (dokuman Modul 12).</summary>
    public class AdminAppointmentListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid ServiceId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public EnumAppointmentStatus Status { get; set; }
        public string? Notes { get; set; }
    }
}
