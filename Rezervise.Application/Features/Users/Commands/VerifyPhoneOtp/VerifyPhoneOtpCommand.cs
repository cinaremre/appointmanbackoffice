using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.VerifyPhoneOtp
{
    /// <summary>
    /// Kayıt sonrası gönderilen telefon doğrulama kodunu (OTP) doğrular.
    /// Başarılı doğrulamada kullanıcı telefon doğrulanmış sayılır ve giriş yapabilir.
    /// </summary>
    public class VerifyPhoneOtpCommand : IRequest<Result<Guid>>
    {
        /// <summary>Kodun gönderildiği telefon numarası.</summary>
        public string PhoneNumber { get; set; }

        /// <summary>Kullanıcı tarafından girilen doğrulama kodu.</summary>
        public string Code { get; set; }
    }
}
