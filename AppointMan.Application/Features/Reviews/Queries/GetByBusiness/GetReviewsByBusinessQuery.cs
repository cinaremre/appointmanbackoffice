using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Reviews.Queries.GetByBusiness
{
    public class GetReviewsByBusinessQuery : IRequest<Result<List<ReviewDto>>>
    {
        public Guid BusinessId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}