using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.AppointmentHistories.Queries.GetByAppointment
{
    public class GetAppointmentHistoryByAppointmentQuery : IRequest<Result<List<AppointmentHistoryDto>>>
    {
        public Guid AppointmentId { get; set; }
    }
}