using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommand : IRequest<Result<Guid>>
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        /// <summary>
        /// Profil güncellemesinde değiştirilecek e-posta. Boş bırakılırsa mevcut
        /// e-posta korunur. Değiştiğinde doğrulama durumu sıfırlanır.
        /// </summary>
        public string? Email { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // Kullanıcının kayıtlı öldüğü il / ilçe.
        // Aramalarda kriter boş geldiğinde varsayılan konum olarak kullanılır.
        public int? CityId { get; set; }
        public int? DistrictId { get; set; }
    }
}