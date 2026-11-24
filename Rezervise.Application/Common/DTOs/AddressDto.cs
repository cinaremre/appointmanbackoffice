namespace Rezervise.Application.Common.DTOs;

/// <summary>
/// İşletme detayında dönen adres nesnesi. Kaydederken gönderilen şekli geri verir:
/// { cityId, street, postalCode, district, city, country }.
/// Uygulama düzenleme ekranında cityId'yi buradan okur; fullAddress tek metin
/// olarak görüntüleme için eklenmiştir.
/// </summary>
public class AddressDto
{
    public int CityId { get; set; }
    public string? City { get; set; }
    public int? DistrictId { get; set; }
    public string? District { get; set; }
    public string? Country { get; set; }
    public string? Street { get; set; }
    public string? PostalCode { get; set; }
    public string? BuildingNumber { get; set; }
    public string? ApartmentNumber { get; set; }

    /// <summary>Tek metin adres (eski GET davranışı ile aynı biçim).</summary>
    public string? FullAddress { get; set; }
}
