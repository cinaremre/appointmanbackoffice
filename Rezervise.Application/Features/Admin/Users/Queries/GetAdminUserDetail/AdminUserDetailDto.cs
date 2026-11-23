using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Users.Queries.GetAdminUserDetail
{
    /// <summary>
    /// Kullanıcı detayı (doküman Modül 11). Gizli alanlar (şifre hash'i,
    /// token hash'leri) bilinçli olarak döndürülmez.
    /// </summary>
    public class AdminUserDetailDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public EnumUserRole Role { get; set; }
        public EnumAccountStatus AccountStatus { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Kullanıcının sahip öldüğü işletmeler.</summary>
        public IReadOnlyList<AdminUserBusinessDto> OwnedBusinesses { get; set; } = Array.Empty<AdminUserBusinessDto>();

        /// <summary>Kullanıcının bağlı çalışan kayıtları (Employee.UserId ilişkisi).</summary>
        public IReadOnlyList<AdminUserEmployeeDto> EmployeePositions { get; set; } = Array.Empty<AdminUserEmployeeDto>();
    }

    /// <summary>Kullanıcının sahibi öldüğü işletmenin özeti.</summary>
    public class AdminUserBusinessDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    /// <summary>Kullanıcının çalışan olarak kayıtlı öldüğü pozisyon özeti.</summary>
    public class AdminUserEmployeeDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
