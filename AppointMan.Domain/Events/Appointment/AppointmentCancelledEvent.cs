namespace AppointMan.Domain.Events.Appointment
{
    public class AppointmentCancelledEvent : AppointmentEventBase
    {
        public string? Reason { get; }
        public string? CancelledBy { get; }

        public AppointmentCancelledEvent(
            Guid appointmentId,
            Guid userId,
            Guid businessId,
            Guid serviceId,
            Guid employeeId,
            DateTime appointmentDate,
            string? reason = null,
            string? cancelledBy = null)
            : base(appointmentId, userId, businessId, serviceId, employeeId, appointmentDate)
        {
            Reason = reason;
            CancelledBy = cancelledBy;
        }
    }
}