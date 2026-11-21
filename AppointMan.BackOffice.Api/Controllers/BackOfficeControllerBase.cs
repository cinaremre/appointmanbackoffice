using AppointMan.BackOffice.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.BackOffice.Api.Controllers
{
    /// <summary>
    /// Backoffice controller'larinin ortak tasiyicisi. Envelope uretimini
    /// tek yerde toplar; controller yalnizca handler cagrisi ve HTTP eslemesi yapar.
    /// Route: base "api/v1/admin" oneki turetilmis controller'lara devrolunur;
    /// kendi [Route] tanimi olan controller'lar (orn. [Route("api/v1/admin/businesses")])
    /// base onekini bilincli olarak degistirir.
    /// </summary>
    [ApiController]
    [Route("api/v1/admin")]
    public abstract class BackOfficeControllerBase : ControllerBase
    {
        /// <summary>Tekil basarili cevabi envelope ile sarar.</summary>
        protected OkObjectResult OkEnvelope<T>(T data)
            => Ok(new AdminResponse<T>
            {
                Data = data,
                Meta = new AdminMeta { CorrelationId = CorrelationId() }
            });

        /// <summary>Liste cevabini sayfalama bilgisiyle sarar.</summary>
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

        /// <summary>Servlet filtre zincirindeki correlation kimligi.</summary>
        private string CorrelationId()
            => Response.Headers.TryGetValue("X-Correlation-ID", out var value)
                ? value.ToString()
                : string.Empty;
    }
}
