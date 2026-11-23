using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.RegisterUser
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

        // Kullanıcının kayıtlı öldüğü il / ilçe.
        // Aramalarda kriter boş geldiğinde varsayılan konum olarak kullanılır.
        public int? CityId { get; set; }
        public int? DistrictId { get; set; }

        // Kullanıcının tercih ettiği dil kodu ("tr" / "en").
        // Boş gelirse varsayılan "tr" kullanılır.
        public string? LanguageCode { get; set; }
    }
}