using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Businesses.Queries.GetBusinessesByOwner
{
    public class GetBusinessesByOwnerQuery : IRequest<Result<List<BusinessDto>>>
    {
        public Guid OwnerId { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}