using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Businesses.Queries.SearchBusinessesByService
{
    public class SearchBusinessesByServiceQuery : IRequest<Result<List<BusinessDto>>>
    {
        // Hizmet filtresi
        public Guid? ServiceCategoryId { get; set; }
        public string? ServiceSearchTerm { get; set; }

        /// <summary>
        /// Birlestirilmis arama terimi: isletme adi, hizmet adi veya sehir adindan
        /// herhangi biri eslesirse isletme listelenir (OR).
        /// Diger filtrelerle (kategori, konum) AND olarak birlesir.
        /// </summary>
        public string? SearchTerm { get; set; }

        // Isletme filtresi (opsiyonel)
        public Guid? BusinessCategoryId { get; set; }

        // Konum filtresi
        public int? CityId { get; set; }
        public int? DistrictId { get; set; }
        public int? LocalityId { get; set; }
        public int? NeighborhoodId { get; set; }

        /// <summary>
        /// Istekte konum bilgisi gelmezse kullanicinin kayitli adresini
        /// kullanabilmek icin gonderilen kullanici ID'si.
        /// </summary>
        public Guid? CurrentUserId { get; set; }

        // Sayfalama
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}