using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Services.Queries.GetServicesByBusiness
{
    public class GetServicesByBusinessQuery : IRequest<Result<List<ServiceDto>>>
    {
        public Guid BusinessId { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}