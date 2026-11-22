using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.ServiceCategories.Queries.GetTree
{
    public class GetServiceCategoryTreeQuery : IRequest<Result<List<ServiceCategoryDto>>>
    {
        public string LanguageCode { get; set; } = "tr";
        public bool OnlyActive { get; set; } = true;
    }
}