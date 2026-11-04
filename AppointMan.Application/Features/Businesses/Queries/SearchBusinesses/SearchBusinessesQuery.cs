using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Businesses.Queries.SearchBusinesses
{
    public class SearchBusinessesQuery : IRequest<Result<List<BusinessDto>>>
    {
        public Guid? CategoryId { get; set; }
        public string City { get; set; }
        public string? District { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}