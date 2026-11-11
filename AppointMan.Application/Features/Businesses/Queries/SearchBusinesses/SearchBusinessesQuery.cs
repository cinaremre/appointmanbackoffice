using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Businesses.Queries.SearchBusinesses
{
    public class SearchBusinessesQuery : IRequest<Result<List<BusinessDto>>>
    {
        public Guid? CategoryId { get; set; }
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