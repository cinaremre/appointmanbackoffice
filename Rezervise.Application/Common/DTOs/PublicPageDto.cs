namespace Rezervise.Application.Common.DTOs
{
    /// <summary>
    /// Anonim erişilebilen public işletme sayfası özeti.
    /// Kişisel veri içermez.
    /// </summary>
    public class PublicPageDto
    {
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public List<PublicPageServiceDto> Services { get; set; } = new();
    }

    /// <summary>Public sayfada listelenen hizmet özeti.</summary>
    public class PublicPageServiceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public int DurationMinutes { get; set; }
    }
}
