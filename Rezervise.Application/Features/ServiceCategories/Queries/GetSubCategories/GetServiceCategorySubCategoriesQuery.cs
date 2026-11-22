using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.ServiceCategories.Queries.GetSubCategories
{
    public class GetServiceCategorySubCategoriesQuery : IRequest<Result<List<ServiceCategoryDto>>>
    {
        public Guid CategoryId { get; set; }
        public string LanguageCode { get; set; } = "tr";
        public bool OnlyActive { get; set; } = true;
    }
}