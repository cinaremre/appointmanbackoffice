using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.BusinessCategories.Queries.GetSubCategories
{
    public class GetSubCategoriesQuery : IRequest<Result<List<BusinessCategoryDto>>>
    {
        public Guid CategoryId { get; set; }
    }
}