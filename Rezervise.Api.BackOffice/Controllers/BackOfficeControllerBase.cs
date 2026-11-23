using Rezervise.BackOffice.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Backoffice controller'larının ortak taşıyıcısı. Envelope üretimini
    /// tek yerde toplar; controller yalnızca handler çağrısı ve HTTP eşlemesi yapar.
    /// Route: base "api/v1/admin" öneki türetilmiş controller'lara devrolünür;
    /// kendi [Route] tanımı olan controller'lar (orn. [Route("api/v1/admin/businesses")])
    /// base önekini bilinçli olarak değiştirir.
    /// </summary>
    [ApiController]
    [Route("api/v1/admin")]
    public abstract class BackOfficeControllerBase : ControllerBase
    {
        /// <summary>Tekil başarılı cevabı envelope ile sarar.</summary>
        protected OkObjectResult OkEnvelope<T>(T data)
            => Ok(new AdminResponse<T>
            {
                Data = data,
                Meta = new AdminMeta { CorrelationId = CorrelationId() }
            });

        /// <summary>Liste cevabını sayfalama bilgisiyle sarar.</summary>
        protected OkObjectResult OkList<T>(
            IReadOnlyList<T> items,
            int page,
            int pageSize,
            long totalCount)
        {
            var totalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

            return Ok(new AdminResponse<IReadOnlyList<T>>
            {
                Data = items,
                Pagination = new PaginationMeta
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasMore = page < totalPages
                },
                Meta = new AdminMeta { CorrelationId = CorrelationId() }
            });
        }

        /// <summary>Servlet filtre zincirindeki correlation kimliği.</summary>
        private string CorrelationId()
            => Response.Headers.TryGetValue("X-Correlation-ID", out var value)
                ? value.ToString()
                : string.Empty;
    }
}
