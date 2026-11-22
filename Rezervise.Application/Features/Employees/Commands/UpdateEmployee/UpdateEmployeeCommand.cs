using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Employees.Commands.UpdateEmployee
{
    public class UpdateEmployeeCommand : IRequest<Result<Guid>>
    {
        public Guid EmployeeId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Title { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public int DisplayOrder { get; set; }
    }
}