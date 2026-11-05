using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.ServiceCategories.Queries.GetTree
{
    public class GetServiceCategoryTreeQuery : IRequest<Result<List<ServiceCategoryDto>>>
    {
        public string LanguageCode { get; set; } = "tr";
        public bool OnlyActive { get; set; } = true;
    }
}