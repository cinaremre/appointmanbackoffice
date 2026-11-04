using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.WorkingHours.Queries.GetByDay
{
    public class GetWorkingHourByDayQuery : IRequest<Result<WorkingHourDto>>
    {
        public Guid BusinessId { get; set; }
        public DayOfWeek Day { get; set; }
    }
}