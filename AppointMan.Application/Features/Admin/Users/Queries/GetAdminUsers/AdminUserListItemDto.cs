using AppointMan.Domain.Enums;

namespace AppointMan.Application.Features.Admin.Users.Queries.GetAdminUsers
{
    /// <summary>
    /// Kullanici listesi ogesi (dokuman Modul 11). Sifre hash'i, refresh
    /// token hash'i ve dogrulama token'lari gibi sifre/secret alanlari
    /// bilincli olarak bulunmaz: gizli veri gorunmez.
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
