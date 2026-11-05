using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.ServiceCategories.Queries.GetFlat
{
    public class GetServiceCategoriesFlatQuery : IRequest<Result<List<ServiceCategoryDto>>>
    {
        public string LanguageCode { get; set; } = "tr";
        public bool OnlyActive { get; set; } = true;
        public bool OnlyLeaves { get; set; } = false;
        public Guid? ParentCategoryId { get; set; }
    }
}