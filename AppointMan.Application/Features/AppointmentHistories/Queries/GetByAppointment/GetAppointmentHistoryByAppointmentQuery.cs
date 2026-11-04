using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.AppointmentHistories.Queries.GetByAppointment
{
    public class GetAppointmentHistoryByAppointmentQuery : IRequest<Result<List<AppointmentHistoryDto>>>
    {
        public Guid AppointmentId { get; set; }
    }
}