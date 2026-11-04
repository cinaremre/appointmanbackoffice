using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.BusinessCategories.Queries.GetAll
{
    public class GetAllCategoriesQuery : IRequest<Result<List<BusinessCategoryDto>>>
    {
        public bool? OnlyActive { get; set; }
    }
}