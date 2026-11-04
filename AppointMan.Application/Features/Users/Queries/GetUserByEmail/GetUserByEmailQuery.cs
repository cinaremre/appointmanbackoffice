using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Users.Queries.GetUserByEmail
{
    public class GetUserByEmailQuery : IRequest<Result<UserDto>>
    {
        public string Email { get; set; }
    }
}