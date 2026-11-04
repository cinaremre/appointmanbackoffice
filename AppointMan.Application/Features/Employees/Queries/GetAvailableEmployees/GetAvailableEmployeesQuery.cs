using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Employees.Queries.GetAvailableEmployees
{
    public class GetAvailableEmployeesQuery : IRequest<Result<List<EmployeeDto>>>
    {
        public Guid BusinessId { get; set; }
        public Guid? ServiceId { get; set; }
        public DateTime? AppointmentDate { get; set; }
    }
}