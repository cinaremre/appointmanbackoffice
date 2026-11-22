namespace Rezervise.Application.Common.DTOs
{
    /// <summary>
    /// Autocomplete sonucu. Id ve ParentId metinsel tutulur cunku adres
    /// entity'leri int, kategori entity'leri Guid anahtar kullanir; mobil
    /// istemci bu degeri ilgili endpoint'e oldugu gibi geri gonderir.
    /// </summary>
    public class AutocompleteItemDto
    {
        /// <summary>Raw entity identifier (int for address data, Guid for categories).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Display name, translated when the source supports localisation.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Source discriminator echoed from the request.</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Parent identifier when the entity belongs to a hierarchy.</summary>
        public string? ParentId { get; set; }

        /// <summary>Parent display name, for breadcrumb rendering on the client.</summary>
        public string? ParentName { get; set; }
    }
}
