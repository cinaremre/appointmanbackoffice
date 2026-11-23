using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.PublicPages.Commands.PublishPublicPage
{
    /// <summary>
    /// İşletmenin public sayfasını yayına alır. İşletmeye ait tek sayfa vardır:
    /// varsa günceller, yoksa oluşturur. Slug başka işletmede kullanılıyorsa Conflict döner.
    /// </summary>
    public class PublishPublicPageCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public string Slug { get; set; } = string.Empty;

        /// <summary>Opsiyonel tema ayarları (JSON metni).</summary>
        public string? ThemeSettings { get; set; }

        /// <summary>Varsayılan true; false verilirse sayfa kaydedilir ama yayına alınmaz.</summary>
        public bool IsPublished { get; set; } = true;
    }
}
