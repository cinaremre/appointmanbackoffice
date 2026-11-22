using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.WorkingHours.Queries.GetByDay
{
    public class GetWorkingHourByDayQuery : IRequest<Result<WorkingHourDto>>
    {
        public Guid BusinessId { get; set; }
        public DayOfWeek Day { get; set; }
    }
}