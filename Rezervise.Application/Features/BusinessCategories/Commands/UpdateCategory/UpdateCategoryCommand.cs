using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.BusinessCategories.Commands.UpdateCategory
{
    public class UpdateCategoryCommand : IRequest<Result<Guid>>
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public EnumBusinessType? CorrespondingBusinessType { get; set; }
        public Guid? ParentCategoryId { get; set; }
    }
}