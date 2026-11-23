namespace Rezervise.Api.Middleware
{
    /// <summary>
    /// Ensures every request carries a correlation id, echoes it back to the caller and
    /// pushes it into the logging scope so error logs can be tied to a response.
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        /// <summary>Header used to propagate the correlation id.</summary>
        public const string HeaderName = "X-Correlation-Id";

        /// <summary>Key used to expose the correlation id through <c>HttpContext.Items</c>.</summary>
        public const string ItemKey = "CorrelationId";

        private const int MaxLength = 64;

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        /// <summary>
        /// İnitialıses the middleware.
        /// </summary>
        /// <param name="next">Next delegate in the pipeline.</param>
        /// <param name="logger">Logger used to open the correlation scope.</param>
        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Creates or reuses a correlation id and continues the pipeline.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        /// <returns>A task representing the middleware execution.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = ResolveCorrelationId(context.Request.Headers[HeaderName].ToString());

            context.Items[ItemKey] = correlationId;
            context.TraceIdentifier = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            using (_logger.BeginScope(new Dictionary<string, object>
                   {
                       [ItemKey] = correlationId
                   }))
            {
                await _next(context);
            }
        }

        /// <summary>
        /// Returns a safe correlation id: the caller supplied value when it is well formed,
        /// otherwise a freshly generated one. This prevents header injection and log forging.
        /// </summary>
        /// <param name="candidate">Caller supplied header value.</param>
        /// <returns>A safe correlation id.</returns>
        private static string ResolveCorrelationId(string? candidate)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= MaxLength)
            {
                var isSafe = true;

                foreach (var character in candidate)
                {
                    if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
                    {
                        isSafe = false;
                        break;
                    }
                }

                if (isSafe)
                    return candidate;
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}
