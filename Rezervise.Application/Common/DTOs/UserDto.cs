using Rezervise.Domain.Enums;

namespace Rezervise.Application.Common.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public EnumUserRole Role { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }

        /// <summary>Kullanıcının kayıtlı ili (arama varsayılanı).</summary>
        public int? CityId { get; set; }

        /// <summary>Kullanıcının kayıtlı ilçesi (arama varsayılanı).</summary>
        public int? DistrictId { get; set; }

        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }
}