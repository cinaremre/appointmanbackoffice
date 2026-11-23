using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.ResendVerificationEmail
{
    /// <summary>
    /// Yeni bir doğrulama token'i üretir ve doğrulama e-postasını tekrar gönderir.
    /// </summary>
    public class ResendVerificationEmailCommand : IRequest<Result<bool>>
    {
        /// <summary>Doğrulanacak hesabın e-posta adresi.</summary>
        public string Email { get; set; } = string.Empty;
    }
}
