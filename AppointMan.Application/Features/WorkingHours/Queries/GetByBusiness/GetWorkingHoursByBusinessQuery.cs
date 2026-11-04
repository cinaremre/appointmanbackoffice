using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.WorkingHours.Queries.GetByBusiness
{
    public class GetWorkingHoursByBusinessQuery : IRequest<Result<List<WorkingHourDto>>>
    {
        public Guid BusinessId { get; set; }
    }
}