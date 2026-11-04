using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Businesses.Queries.GetBusinessById
{
    public class GetBusinessByIdQuery : IRequest<Result<BusinessDto>>
    {
        public Guid BusinessId { get; set; }
    }
}