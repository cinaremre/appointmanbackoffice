namespace Rezervise.Domain.Events.Appointment
{
    public class AppointmentCompletedEvent : AppointmentEventBase
    {
        public string? CompletedBy { get; }

        public AppointmentCompletedEvent(
            Guid appointmentId,
            Guid userId,
            Guid businessId,
            Guid serviceId,
            Guid employeeId,
            DateTime appointmentDate,
            string? completedBy = null)
            : base(appointmentId, userId, businessId, serviceId, employeeId, appointmentDate)
        {
            CompletedBy = completedBy;
        }
    }
}