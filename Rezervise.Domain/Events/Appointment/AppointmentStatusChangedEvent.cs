using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Events.Appointment
{
    public class AppointmentStatusChangedEvent : DomainEventBase
    {
        public Guid AppointmentId { get; }
        public Guid UserId { get; }
        public Guid BusinessId { get; }
        public Guid EmployeeId { get; }
        public EnumAppointmentStatus OldStatus { get; }
        public EnumAppointmentStatus NewStatus { get; }
        public string? ChangedBy { get; }
        public string? ChangeReason { get; }

        public AppointmentStatusChangedEvent(
            Guid appointmentId,
            Guid userId,
            Guid businessId,
            Guid employeeId,
            EnumAppointmentStatus oldStatus,
            EnumAppointmentStatus newStatus,
            string? changedBy = null,
            string? changeReason = null) : base()
        {
            AppointmentId = appointmentId;
            UserId = userId;
            BusinessId = businessId;
            EmployeeId = employeeId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            ChangedBy = changedBy;
            ChangeReason = changeReason;
        }
    }
}