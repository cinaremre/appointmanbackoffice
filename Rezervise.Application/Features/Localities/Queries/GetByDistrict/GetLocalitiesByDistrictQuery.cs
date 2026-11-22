using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Localities.Queries.GetByDistrict
{
    public class GetLocalitiesByDistrictQuery : IRequest<Result<List<LocalityDto>>>
    {
        public int DistrictId { get; set; }
        public bool? OnlyActive { get; set; }
    }
}
