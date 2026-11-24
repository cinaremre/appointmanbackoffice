using Rezervise.Domain.Enums;

namespace Rezervise.Application.Common.DTOs;

/// <summary>
/// İşletme DETAY yanıtı (GET /api/Business/{id}). Kaydederken gönderilen
/// şekli geri verir: address nesne, phoneNumber/email { "value": "..." }.
/// Liste endpoint'leri görüntüleme için BusinessDto (tek metin adres) kullanmaya devam eder.
/// </summary>
public class BusinessDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public EnumBusinessType BusinessType { get; set; }
    public AddressDto? Address { get; set; }
    public ContactValueDto? PhoneNumber { get; set; }
    public ContactValueDto? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }

    /// <summary>Backoffice onayı: false ise işletme henüz onaylanmamıştır.</summary>
    public bool IsApproved { get; set; }
    public decimal? Rating { get; set; }
    public int TotalReviews { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
}
