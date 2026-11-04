using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Employees.Commands.CreateEmployee
{
    public class CreateEmployeeCommand : IRequest<Result<Guid>>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Title { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }
}