using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.BusinessCategories.Commands.DeleteCategory
{
    public class DeleteCategoryCommand : IRequest<Result<Guid>>
    {
        public Guid CategoryId { get; set; }
    }
}