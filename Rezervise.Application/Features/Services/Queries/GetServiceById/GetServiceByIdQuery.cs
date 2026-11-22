using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Services.Queries.GetServiceById
{
    public class GetServiceByIdQuery : IRequest<Result<ServiceDto>>
    {
        public Guid ServiceId { get; set; }
    }
}