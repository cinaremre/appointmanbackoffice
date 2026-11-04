using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.BusinessCategories.Queries.GetSubCategories
{
    public class GetSubCategoriesQuery : IRequest<Result<List<BusinessCategoryDto>>>
    {
        public Guid CategoryId { get; set; }
    }
}