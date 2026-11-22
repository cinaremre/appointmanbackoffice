namespace Rezervise.Domain.Events.Employee
{
    public class EmployeeDeactivatedEvent : DomainEventBase
    {
        public Guid EmployeeId { get; }
        public Guid BusinessId { get; }
        public string FullName { get; }
        public string? Reason { get; }

        public EmployeeDeactivatedEvent(Guid employeeId, Guid businessId, string fullName, string? reason) : base()
        {
            EmployeeId = employeeId;
            BusinessId = businessId;
            FullName = fullName;
            Reason = reason;
        }
    }
}
