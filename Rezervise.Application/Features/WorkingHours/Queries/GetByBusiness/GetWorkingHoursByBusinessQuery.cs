using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.WorkingHours.Queries.GetByBusiness
{
    public class GetWorkingHoursByBusinessQuery : IRequest<Result<List<WorkingHourDto>>>
    {
        public Guid BusinessId { get; set; }
    }
}