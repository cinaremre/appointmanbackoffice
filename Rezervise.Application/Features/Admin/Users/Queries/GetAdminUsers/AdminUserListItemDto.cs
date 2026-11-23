using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Users.Queries.GetAdminUsers
{
    /// <summary>
    /// Kullanıcı listesi öğesi (doküman Modül 11). Şifre hash'i, refresh
    /// token hash'i ve doğrulama token'lari gibi şifre/secret alanları
    /// bilinçli olarak bulunmaz: gizli veri görünmez.
    /// </summary>
    public class AdminUserListItemDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public EnumUserRole Role { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
