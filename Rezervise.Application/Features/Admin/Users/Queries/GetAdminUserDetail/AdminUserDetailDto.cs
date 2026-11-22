using Rezervise.Domain.Enums;

namespace Rezervise.Application.Features.Admin.Users.Queries.GetAdminUserDetail
{
    /// <summary>
    /// Kullanici detayi (dokuman Modul 11). Gizli alanlar (sifre hash'i,
    /// token hash'leri) bilincli olarak dondurulmez.
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

        /// <summary>Kullanicinin sahip oldugu isletmeler.</summary>
        public IReadOnlyList<AdminUserBusinessDto> OwnedBusinesses { get; set; } = Array.Empty<AdminUserBusinessDto>();

        /// <summary>Kullanicinin bagli calisan kayitlari (Employee.UserId iliskisi).</summary>
        public IReadOnlyList<AdminUserEmployeeDto> EmployeePositions { get; set; } = Array.Empty<AdminUserEmployeeDto>();
    }

    /// <summary>Kullanicinin sahibi oldugu isletmenin ozeti.</summary>
    public class AdminUserBusinessDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    /// <summary>Kullanicinin calisan olarak kayitli oldugu pozisyon ozeti.</summary>
    public class AdminUserEmployeeDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
