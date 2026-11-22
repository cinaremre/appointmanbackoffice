namespace Rezervise.Domain.Events.Appointment
{
    public abstract class AppointmentEventBase : DomainEventBase
    {
        public Guid AppointmentId { get; }
        public Guid UserId { get; }
        public Guid BusinessId { get; }
        public Guid ServiceId { get; }
        public Guid EmployeeId { get; }
        public DateTime AppointmentDate { get; }

        protected AppointmentEventBase(
            Guid appointmentId,
            Guid userId,
            Guid businessId,
            Guid serviceId,
            Guid employeeId,
            DateTime appointmentDate) : base()
        {
            AppointmentId = appointmentId;
            UserId = userId;
            BusinessId = businessId;
            ServiceId = serviceId;
            EmployeeId = employeeId;
            AppointmentDate = appointmentDate;
        }
    }
}