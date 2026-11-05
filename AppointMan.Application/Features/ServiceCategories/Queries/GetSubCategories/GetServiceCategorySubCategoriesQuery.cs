using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.ServiceCategories.Queries.GetSubCategories
{
    public class GetServiceCategorySubCategoriesQuery : IRequest<Result<List<ServiceCategoryDto>>>
    {
        public Guid CategoryId { get; set; }
        public string LanguageCode { get; set; } = "tr";
        public bool OnlyActive { get; set; } = true;
    }
}