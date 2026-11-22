using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.BusinessCategories.Queries.GetById
{
    public class GetCategoryByIdQuery : IRequest<Result<BusinessCategoryDto>>
    {
        public Guid CategoryId { get; set; }
    }
}