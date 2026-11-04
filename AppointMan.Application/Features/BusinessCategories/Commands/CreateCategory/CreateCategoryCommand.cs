using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.BusinessCategories.Commands.CreateCategory
{
    public class CreateCategoryCommand : IRequest<Result<Guid>>
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public EnumBusinessType? CorrespondingBusinessType { get; set; }
        public Guid? ParentCategoryId { get; set; }
    }
}