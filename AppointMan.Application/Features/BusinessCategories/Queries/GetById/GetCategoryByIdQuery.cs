using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.BusinessCategories.Queries.GetById
{
    public class GetCategoryByIdQuery : IRequest<Result<BusinessCategoryDto>>
    {
        public Guid CategoryId { get; set; }
    }
}