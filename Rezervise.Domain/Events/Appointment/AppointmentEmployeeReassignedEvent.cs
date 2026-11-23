namespace Rezervise.Domain.Events.Appointment
{
    /// <summary>
    /// Randevunun çalışanının değiştiğini bildirir.
    /// </summary>
    public class AppointmentEmployeeReassignedEvent : AppointmentEventBase
    {
        public Guid OldEmployeeId { get; }
        public Guid NewEmployeeId { get; }
        public string? ChangedBy { get; }

        public AppointmentEmployeeReassignedEvent(
            Guid appointmentId,
            Guid userId,
            Guid businessId,
            Guid serviceId,
            Guid oldEmployeeId,
            Guid newEmployeeId,
            DateTime appointmentDate,
            string? changedBy = null)
            : base(appointmentId, userId, businessId, serviceId, newEmployeeId, appointmentDate)
        {
            OldEmployeeId = oldEmployeeId;
            NewEmployeeId = newEmployeeId;
            ChangedBy = changedBy;
        }
    }
}
