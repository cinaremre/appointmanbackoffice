using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Users.Commands.VerifyPhoneOtp
{
    /// <summary>
    /// Kayit sonrasi gonderilen telefon dogrulama kodunu (OTP) dogrular.
    /// Basarili dogrulamada kullanici telefon dogrulanmis sayilir ve giris yapabilir.
    /// </summary>
    public class VerifyPhoneOtpCommand : IRequest<Result<Guid>>
    {
        /// <summary>Kodun gonderildigi telefon numarasi.</summary>
        public string PhoneNumber { get; set; }

        /// <summary>Kullanici tarafindan girilen dogrulama kodu.</summary>
        public string Code { get; set; }
    }
}
