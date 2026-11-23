using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.Login
{
    public class LoginCommand : IRequest<Result<LoginResponse>>
    {
        /// <summary>
        /// Login identifier. Accepts either an e-mail address or a phone number.
        /// </summary>
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expiration { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }

        /// <summary>Kullanıcının tercih ettiği dil kodu ("tr" / "en").</summary>
        public string LanguageCode { get; set; }
    }
}