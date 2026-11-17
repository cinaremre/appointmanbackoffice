using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Users.Commands.ResendVerificationEmail
{
    /// <summary>
    /// Yeni bir dogrulama token'i uretir ve dogrulama e-postasini tekrar gonderir.
    /// </summary>
    public class ResendVerificationEmailCommand : IRequest<Result<bool>>
    {
        /// <summary>Dogrulanacak hesabin e-posta adresi.</summary>
        public string Email { get; set; } = string.Empty;
    }
}
