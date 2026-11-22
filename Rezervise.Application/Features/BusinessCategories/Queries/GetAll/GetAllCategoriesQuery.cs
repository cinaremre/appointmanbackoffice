using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.BusinessCategories.Queries.GetAll
{
    public class GetAllCategoriesQuery : IRequest<Result<List<BusinessCategoryDto>>>
    {
        public bool? OnlyActive { get; set; }
    }
}