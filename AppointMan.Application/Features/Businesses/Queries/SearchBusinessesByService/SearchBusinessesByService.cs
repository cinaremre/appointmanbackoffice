using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Businesses.Queries.SearchBusinessesByService
{
    public class SearchBusinessesByServiceQuery : IRequest<Result<List<BusinessDto>>>
    {
        // Hizmet filtresi
        public Guid? ServiceCategoryId { get; set; }
        public string? ServiceSearchTerm { get; set; }

        // Isletme filtresi (opsiyonel)
        public Guid? BusinessCategoryId { get; set; }

        // Konum filtresi
        public string City { get; set; }
        public string? District { get; set; }

        // Sayfalama
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}