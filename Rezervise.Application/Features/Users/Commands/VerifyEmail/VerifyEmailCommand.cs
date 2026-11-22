using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.VerifyEmail
{
    public class VerifyEmailCommand : IRequest<Result<Guid>>
    {
        public Guid UserId { get; set; }
        public string VerificationCode { get; set; }
    }
}
