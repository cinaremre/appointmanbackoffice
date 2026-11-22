using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.UpdateUserLanguage
{
    /// <summary>
    /// Kullanicinin dil tercihini gunceller. Sonraki isteklerde kullaniciya
    /// donen metinler bu dile gore cozulur.
    /// </summary>
    public class UpdateUserLanguageCommand : IRequest<Result<Guid>>
    {
        /// <summary>Dili guncellenecek kullanici.</summary>
        public Guid UserId { get; set; }

        /// <summary>Yeni dil kodu ("tr" / "en").</summary>
        public string LanguageCode { get; set; }
    }
}
