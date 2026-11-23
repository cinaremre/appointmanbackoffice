using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.PublicPages.Queries.GetPublicPageBySlug
{
    /// <summary>
    /// Slug ile public sayfa özetini getirir. ANONIM üçtür:
    /// handler hiçbir şekilde ICurrentUserService kullanmaz.
    /// Yayında olmayan sayfa NotFound döner; varlık bilgisi sızdırılmaz.
    /// </summary>
    public class GetPublicPageBySlugQuery : IRequest<Result<PublicPageDto>>
    {
        public string Slug { get; set; } = string.Empty;
    }
}
