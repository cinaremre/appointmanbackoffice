namespace Rezervise.Domain.Events.Appointment
{
    public class AppointmentConfirmedEvent : AppointmentEventBase
    {
        public string? ConfirmedBy { get; }

        public AppointmentConfirmedEvent(
            Guid appointmentId,
            Guid userId,
            Guid businessId,
            Guid serviceId,
            Guid employeeId,
            DateTime appointmentDate,
            string? confirmedBy = null)
            : base(appointmentId, userId, businessId, serviceId, employeeId, appointmentDate)
        {
            ConfirmedBy = confirmedBy;
        }
    }
}