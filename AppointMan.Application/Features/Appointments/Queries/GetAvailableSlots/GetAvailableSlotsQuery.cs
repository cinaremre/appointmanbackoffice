using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Appointments.Queries.GetAvailableSlots
{
    public class GetAvailableSlotsQuery : IRequest<Result<List<AvailableSlotDto>>>
    {
        public Guid BusinessId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid ServiceId { get; set; }
        public DateTime Date { get; set; }   // Hangi gun icin
    }
}