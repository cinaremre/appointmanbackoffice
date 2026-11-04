using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Employees.Commands.DeactivateEmployee
{
    public class DeactivateEmployeeCommand : IRequest<Result<Guid>>
    {
        public Guid EmployeeId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
        public string? Reason { get; set; }
    }
}