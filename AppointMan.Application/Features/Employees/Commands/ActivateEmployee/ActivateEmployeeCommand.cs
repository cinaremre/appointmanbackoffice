using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Employees.Commands.ActivateEmployee
{
    public class ActivateEmployeeCommand : IRequest<Result<Guid>>
    {
        public Guid EmployeeId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
    }
}