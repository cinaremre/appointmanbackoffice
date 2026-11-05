using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.ServiceCategories.Queries.GetById
{
    public class GetServiceCategoryByIdQuery : IRequest<Result<ServiceCategoryDto>>
    {
        public Guid CategoryId { get; set; }
        public string LanguageCode { get; set; } = "tr";
    }
}