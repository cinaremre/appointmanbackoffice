using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.BusinessCategories.Commands.DeleteCategory
{
    public class DeleteCategoryCommand : IRequest<Result<Guid>>
    {
        public Guid CategoryId { get; set; }
    }
}