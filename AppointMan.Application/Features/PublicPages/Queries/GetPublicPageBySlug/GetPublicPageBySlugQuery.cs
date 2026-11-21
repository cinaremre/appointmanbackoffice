using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.PublicPages.Queries.GetPublicPageBySlug
{
    /// <summary>
    /// Slug ile public sayfa ozetini getirir. ANONIM uctur:
    /// handler hicbir sekilde ICurrentUserService kullanmaz.
    /// Yayinda olmayan sayfa NotFound doner; varlik bilgisi sizdirilmaz.
    /// </summary>
    public class GetPublicPageBySlugQuery : IRequest<Result<PublicPageDto>>
    {
        public string Slug { get; set; } = string.Empty;
    }
}
