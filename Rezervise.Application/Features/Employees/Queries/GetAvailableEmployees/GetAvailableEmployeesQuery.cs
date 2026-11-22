using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Employees.Queries.GetAvailableEmployees
{
    public class GetAvailableEmployeesQuery : IRequest<Result<List<EmployeeDto>>>
    {
        public Guid BusinessId { get; set; }
        public Guid? ServiceId { get; set; }
        public DateTime? AppointmentDate { get; set; }
    }
}