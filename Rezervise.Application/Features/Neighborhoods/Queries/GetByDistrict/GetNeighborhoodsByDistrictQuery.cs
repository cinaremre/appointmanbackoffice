using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Neighborhoods.Queries.GetByDistrict
{
    /// <summary>
    /// Bir ilçeye bağlı tüm mahalleleri (semt ayrımı yapmadan) getirir.
    /// Mobil adres seçiminde ilçe seçildikten sonra tek çağrıyla mahalle listesi alınır.
    /// </summary>
    public class GetNeighborhoodsByDistrictQuery : IRequest<Result<List<NeighborhoodDto>>>
    {
        public int DistrictId { get; set; }
        public bool? OnlyActive { get; set; }
    }
}
