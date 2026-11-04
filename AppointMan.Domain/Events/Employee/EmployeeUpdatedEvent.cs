namespace AppointMan.Domain.Events.Employee
{
    public class EmployeeUpdatedEvent : DomainEventBase
    {
        public Guid EmployeeId { get; }
        public Guid BusinessId { get; }
        public string FullName { get; }

        public EmployeeUpdatedEvent(Guid employeeId, Guid businessId, string fullName) : base()
        {
            EmployeeId = employeeId;
            BusinessId = businessId;
            FullName = fullName;
        }
    }
}
