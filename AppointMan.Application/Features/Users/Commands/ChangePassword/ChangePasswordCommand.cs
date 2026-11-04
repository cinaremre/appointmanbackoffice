using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Users.Commands.ChangePassword
{
    public class ChangePasswordCommand : IRequest<Result<Guid>>
    {
        public Guid UserId { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}