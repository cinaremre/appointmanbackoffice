using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Services.Queries.SearchServices
{
    public class SearchServicesQuery : IRequest<Result<List<ServiceDto>>>
    {
        public Guid? BusinessId { get; set; }
        public Guid? ServiceCategoryId { get; set; }
        public bool? IsActive { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}