using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Users.Commands.VerifyEmailToken
{
    /// <summary>
    /// E-posta adresini, e-posta ile gonderilen token'i kullanarak dogrular.
    /// Kimlik dogrulamasi gerektirmez; token'in kendisi yetkidir.
    /// </summary>
    public class VerifyEmailTokenCommand : IRequest<Result<bool>>
    {
        /// <summary>Baglantidan gelen ham (hash'lenmemis) dogrulama token'i.</summary>
        public string Token { get; set; } = string.Empty;
    }
}
