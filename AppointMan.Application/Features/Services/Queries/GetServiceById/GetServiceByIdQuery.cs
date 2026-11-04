using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Services.Queries.GetServiceById
{
    public class GetServiceByIdQuery : IRequest<Result<ServiceDto>>
    {
        public Guid ServiceId { get; set; }
    }
}