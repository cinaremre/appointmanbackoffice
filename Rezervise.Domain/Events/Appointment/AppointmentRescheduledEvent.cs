namespace Rezervise.Domain.Events.Appointment
{
    public class AppointmentRescheduledEvent : AppointmentEventBase
    {
        public DateTime OldDate { get; }
        public DateTime NewDate { get; }
        public string? RescheduledBy { get; }

        public AppointmentRescheduledEvent(
            Guid appointmentId,
            Guid userId,
            Guid businessId,
            Guid serviceId,
            Guid employeeId,
            DateTime oldDate,
            DateTime newDate,
            string? rescheduledBy = null)
            : base(appointmentId, userId, businessId, serviceId, employeeId, newDate)
        {
            OldDate = oldDate;
            NewDate = newDate;
            RescheduledBy = rescheduledBy;
        }
    }
}