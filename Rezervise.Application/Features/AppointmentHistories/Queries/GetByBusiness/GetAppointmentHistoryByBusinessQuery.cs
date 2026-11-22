using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.AppointmentHistories.Queries.GetByBusiness
{
    public class GetAppointmentHistoryByBusinessQuery : IRequest<Result<List<AppointmentHistoryDto>>>
    {
        public Guid BusinessId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}