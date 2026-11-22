using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Employees.Queries.GetEmployeesByBusiness
{
    public class GetEmployeesByBusinessQuery : IRequest<Result<List<EmployeeDto>>>
    {
        public Guid BusinessId { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}