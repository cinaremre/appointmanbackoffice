using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Services.Commands.CreateService
{
    public class CreateServiceCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "TRY";
        public int DurationInMinutes { get; set; }
        public Guid ServiceCategoryId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
    }
}