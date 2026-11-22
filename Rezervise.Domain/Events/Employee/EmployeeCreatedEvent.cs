namespace Rezervise.Domain.Events.Employee
{
    public class EmployeeCreatedEvent : DomainEventBase
    {
        public Guid EmployeeId { get; }
        public Guid BusinessId { get; }
        public string FullName { get; }

        public EmployeeCreatedEvent(Guid employeeId, Guid businessId, string fullName) : base()
        {
            EmployeeId = employeeId;
            BusinessId = businessId;
            FullName = fullName;
        }
    }
}