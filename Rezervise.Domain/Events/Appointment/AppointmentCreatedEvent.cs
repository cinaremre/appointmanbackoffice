namespace Rezervise.Domain.Events.Appointment
{
    public class AppointmentCreatedEvent : AppointmentEventBase
    {
        public Guid EmployeeId { get; }
        public string? Notes { get; }

        public AppointmentCreatedEvent(
            Guid appointmentId,
            Guid userId,
            Guid businessId,
            Guid serviceId,
            Guid employeeId,
            DateTime appointmentDate,
            string? notes = null)
            : base(appointmentId, userId, businessId, serviceId, employeeId, appointmentDate)
        {
            EmployeeId = employeeId;
            Notes = notes;
        }
    }
}