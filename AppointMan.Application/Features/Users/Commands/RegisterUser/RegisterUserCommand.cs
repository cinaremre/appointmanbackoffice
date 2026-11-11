using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommand : IRequest<Result<Guid>>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public EnumUserRole Role { get; set; } = EnumUserRole.Customer;
        public DateTime? DateOfBirth { get; set; }

        // Kullanicinin kayitli oldugu il / ilce.
        // Aramalarda kriter bos geldiginde varsayilan konum olarak kullanilir.
        public int? CityId { get; set; }
        public int? DistrictId { get; set; }

        // Kullanicinin tercih ettigi dil kodu ("tr" / "en").
        // Bos gelirse varsayilan "tr" kullanilir.
        public string? LanguageCode { get; set; }
    }
}