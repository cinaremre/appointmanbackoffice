using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Districts.Queries.GetByCity
{
    public class GetDistrictsByCityQuery : IRequest<Result<List<DistrictDto>>>
    {
        public int CityId { get; set; }
        public bool? OnlyActive { get; set; }
    }
}
