using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Businesses.Queries.GetBusinessById
{
    public class GetBusinessByIdQuery : IRequest<Result<BusinessDto>>
    {
        public Guid BusinessId { get; set; }
    }
}