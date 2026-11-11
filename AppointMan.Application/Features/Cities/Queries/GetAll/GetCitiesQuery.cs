using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Cities.Queries.GetAll
{
    public class GetCitiesQuery : IRequest<Result<List<CityDto>>>
    {
        public bool? OnlyActive { get; set; }
    }
}
