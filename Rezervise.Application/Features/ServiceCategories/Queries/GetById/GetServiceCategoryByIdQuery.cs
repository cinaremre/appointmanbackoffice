using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.ServiceCategories.Queries.GetById
{
    public class GetServiceCategoryByIdQuery : IRequest<Result<ServiceCategoryDto>>
    {
        public Guid CategoryId { get; set; }
        public string LanguageCode { get; set; } = "tr";
    }
}