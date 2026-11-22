using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.PublicPages.Commands.PublishPublicPage
{
    /// <summary>
    /// Isletmenin public sayfasini yayina alir. Isletmeye ait tek sayfa vardir:
    /// varsa gunceller, yoksa olusturur. Slug baska isletmede kullaniliyorsa Conflict doner.
    /// </summary>
    public class PublishPublicPageCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public string Slug { get; set; } = string.Empty;

        /// <summary>Opsiyonel tema ayarlari (JSON metni).</summary>
        public string? ThemeSettings { get; set; }

        /// <summary>Varsayilan true; false verilirse sayfa kaydedilir ama yayina alinmaz.</summary>
        public bool IsPublished { get; set; } = true;
    }
}
