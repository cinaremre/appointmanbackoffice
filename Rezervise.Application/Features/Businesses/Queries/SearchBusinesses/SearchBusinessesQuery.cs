using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Businesses.Queries.SearchBusinesses
{
    public class SearchBusinessesQuery : IRequest<Result<List<BusinessDto>>>
    {
        public Guid? CategoryId { get; set; }

        /// <summary>
        /// Sehir adi (orn. "Ankara"). CityId verilmemisse bu ada gore ID cozulur.
        /// CityId doluysa dikkate alinmaz.
        /// </summary>
        public string? City { get; set; }

        public int? CityId { get; set; }
        public int? DistrictId { get; set; }
        public int? LocalityId { get; set; }
        public int? NeighborhoodId { get; set; }

        /// <summary>
        /// Istekte konum bilgisi gelmezse kullanicinin kayitli adresini
        /// kullanabilmek icin gonderilen kullanici ID'si.
        /// </summary>
        public Guid? CurrentUserId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}