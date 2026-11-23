using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.UpdateUserLanguage
{
    /// <summary>
    /// Kullanıcının dil tercihini günceller. Sonraki isteklerde kullanıcıya
    /// dönen metinler bu dile göre çözülür.
    /// </summary>
    public class UpdateUserLanguageCommand : IRequest<Result<Guid>>
    {
        /// <summary>Dili güncellenecek kullanıcı.</summary>
        public Guid UserId { get; set; }

        /// <summary>Yeni dil kodu ("tr" / "en").</summary>
        public string LanguageCode { get; set; }
    }
}
