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
        /// Birleştirilmiş arama terimi: işletme adı, hizmet adı veya şehir adından
        /// herhangi biri eşleşirse işletme listelenir (OR).
        /// Diğer filtrelerle (kategori, konum) AND olarak birleşir.
        /// </summary>
        public string? SearchTerm { get; set; }

        // İşletme filtresi (opsiyonel)
        public Guid? BusinessCategoryId { get; set; }

        // Konum filtresi
        public int? CityId { get; set; }
        public int? DistrictId { get; set; }
        public int? LocalityId { get; set; }
        public int? NeighborhoodId { get; set; }

        /// <summary>
        /// İstekte konum bilgisi gelmezse kullanıcının kayıtlı adresini
        /// kullanabilmek için gönderilen kullanıcı ID'si.
        /// </summary>
        public Guid? CurrentUserId { get; set; }

        // Sayfalama
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}