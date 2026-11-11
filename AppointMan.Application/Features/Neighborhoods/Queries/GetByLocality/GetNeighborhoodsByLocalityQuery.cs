using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Neighborhoods.Queries.GetByLocality
{
    public class GetNeighborhoodsByLocalityQuery : IRequest<Result<List<NeighborhoodDto>>>
    {
        public int LocalityId { get; set; }
        public bool? OnlyActive { get; set; }
    }
}
