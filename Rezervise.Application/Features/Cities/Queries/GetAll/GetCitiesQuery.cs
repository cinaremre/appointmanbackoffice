using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Cities.Queries.GetAll
{
    public class GetCitiesQuery : IRequest<Result<List<CityDto>>>
    {
        public bool? OnlyActive { get; set; }
    }
}
