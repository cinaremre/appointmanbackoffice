using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Neighborhoods.Queries.GetByLocality
{
    public class GetNeighborhoodsByLocalityQuery : IRequest<Result<List<NeighborhoodDto>>>
    {
        public int LocalityId { get; set; }
        public bool? OnlyActive { get; set; }
    }
}
