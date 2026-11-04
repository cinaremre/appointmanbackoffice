using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Appointments.Queries.GetAppointmentsByBusiness
{
    public class GetAppointmentsByBusinessQuery : IRequest<Result<List<AppointmentDto>>>
    {
        public Guid BusinessId { get; set; }
        public Guid? EmployeeId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}