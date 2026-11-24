namespace Rezervise.Application.Common.DTOs;

/// <summary>
/// Telefon/e-posta alanları value object olduğu için JSON'da { "value": "..." }
/// biçiminde döner; uygulama doğrudan string beklediği için nesne sözerlidir.
/// </summary>
public class ContactValueDto
{
    public ContactValueDto()
    {
    }

    public ContactValueDto(string? value)
        => Value = value;

    public string? Value { get; set; }
}
