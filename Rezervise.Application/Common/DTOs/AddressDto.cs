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

    /// <summary>
    /// Semt (locality) kimliği. Kayıtta gönderilmediyse null döner.
    /// </summary>
    public int? LocalityId { get; set; }

    /// <summary>
    /// Mahalle kimliği. Kayıtta gönderilmediyse null döner.
    /// </summary>
    public int? NeighborhoodId { get; set; }

    public string? Country { get; set; }
    public string? Street { get; set; }
    public string? PostalCode { get; set; }
    public string? BuildingNumber { get; set; }
    public string? ApartmentNumber { get; set; }

    /// <summary>
    /// Adresin koordinatları (kaydederken gönderilen şekil):
    /// { "latitude": 40.987, "longitude": 29.025 }. Koordinat yoksa null.
    /// </summary>
    public CoordinatesDto? Coordinates { get; set; }

    /// <summary>Tek metin adres (eski GET davranışı ile aynı biçim).</summary>
    public string? FullAddress { get; set; }
}

/// <summary>
/// Enlem/boylam çifti. Adres kaydındaki <c>coordinates</c> nesnesiyle birebir
/// aynı alan adlarını kullanır; mobil düzenleme ekranı buradan okur.
/// </summary>
public class CoordinatesDto
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
