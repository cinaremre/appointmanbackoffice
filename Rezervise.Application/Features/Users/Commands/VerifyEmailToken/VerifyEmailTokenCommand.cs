using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.VerifyEmailToken
{
    /// <summary>
    /// E-posta adresini, e-posta ile gönderilen token'i kullanarak doğrular.
    /// Kimlik doğrulaması gerektirmez; token'in kendisi yetkidir.
    /// </summary>
    public class VerifyEmailTokenCommand : IRequest<Result<bool>>
    {
        /// <summary>Bağlantıdan gelen ham (hash'lenmemiş) doğrulama token'i.</summary>
        public string Token { get; set; } = string.Empty;
    }
}
