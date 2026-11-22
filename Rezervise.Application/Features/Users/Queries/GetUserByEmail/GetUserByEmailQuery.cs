using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Users.Queries.GetUserByEmail
{
    public class GetUserByEmailQuery : IRequest<Result<UserDto>>
    {
        public string Email { get; set; }
    }
}