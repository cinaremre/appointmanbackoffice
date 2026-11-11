using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Localities.Queries.GetByDistrict
{
    public class GetLocalitiesByDistrictQuery : IRequest<Result<List<LocalityDto>>>
    {
        public int DistrictId { get; set; }
        public bool? OnlyActive { get; set; }
    }
}
