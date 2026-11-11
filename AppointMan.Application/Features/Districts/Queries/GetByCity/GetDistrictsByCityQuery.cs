using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Districts.Queries.GetByCity
{
    public class GetDistrictsByCityQuery : IRequest<Result<List<DistrictDto>>>
    {
        public int CityId { get; set; }
        public bool? OnlyActive { get; set; }
    }
}
