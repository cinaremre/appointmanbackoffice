using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Services.Commands.UpdateService
{
    public class UpdateServiceCommand : IRequest<Result<Guid>>
    {
        public Guid ServiceId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "TRY";
        public int DurationInMinutes { get; set; }
        public EnumServiceCategory Category { get; set; }
    }
}