using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using Rezervise.Api.Logging;
using Microsoft.Extensions.Configuration;

namespace Rezervise.Api.Middleware
{
    /// <summary>
    /// Writes one structured entry per HTTP request containing the method, url, query, masked
    /// request and response bodies, status code, duration, caller identity and correlation id.
    /// The middleware is fail-safe: a logging problem can never alter the response, and the
    /// original response stream is always restored before the response is completed.
    /// </summary>
    public sealed class RequestResponseLoggingMiddleware
    {
        /// <summary>
        /// Message template used for every entry. Each placeholder becomes a dedicated GELF field
        /// (<c>_correlationId</c>, <c>_requestBody</c>, ...) because the sink expands log state.
        /// </summary>
        private const string LogTemplate =
            "HTTP {HttpMethod} {Url} responded {StatusCode} in {DurationMs}ms | " +
            "correlationId={CorrelationId} userId={UserId} query={Query} " +
            "requestBody={RequestBody} responseBody={ResponseBody}";

        private const string UserIdClaimType = "userId";

        /// <summary>
        /// Key under which the masked request body is exposed through <c>HttpContext.Items</c>
        /// so the exception middleware can include it without reading the stream twice.
        /// </summary>
        public const string RequestBodyItemKey = "HttpRequestBody";

        /// <summary>Maximum captured body size; anything larger is summarized instead of logged.</summary>
        private const int MaxBodyBytes = SensitiveDataMasker.DefaultMaxBytes;

        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly IReadOnlyCollection<string> _sensitiveFields;

        /// <summary>
        /// İnitialıses the middleware.
        /// </summary>
        /// <param name="next">Next delegate in the pipeline.</param>
        /// <param name="logger">Logger receiving the request/response entry.</param>
        /// <param name="configuration">Configuration used to resolve the masking field list.</param>
        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestResponseLoggingMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _sensitiveFields = ResolveSensitiveFields(configuration);
        }

        /// <summary>
        /// Captures and logs the request/response pair, then continues the pipeline.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        /// <returns>A task representing the middleware execution.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var startedAt = Stopwatch.GetTimestamp();

            string? requestBody = null;
            string? responseBody = null;

            var originalBody = context.Response.Body;
            var buffer = new MemoryStream();

            try
            {
                requestBody = await ReadRequestBodyAsync(context);

                // Expose the already masked body for the exception middleware that runs inside this one.
                context.Items[RequestBodyItemKey] = requestBody;

                // The response is buffered so it can be inspected before reaching the client.
                context.Response.Body = buffer;

                await _next(context);

                responseBody = ReadResponseBody(context, buffer);
            }
            catch (Exception ex)
            {
                // Anything escaping the pipeline is logged here and rethrown so the outer
                // exception handler can still translate it into a response.
                Log(context, startedAt, requestBody, responseBody, ex);

                throw;
            }
            finally
            {
                await FlushAndRestoreAsync(context, buffer, originalBody);
            }

            Log(context, startedAt, requestBody, responseBody, exception: null);
        }

        /// <summary>
        /// Copies the buffered response to the real stream and always restores the original body.
        /// A failure here must not mask the response already produced by the pipeline.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        /// <param name="buffer">Buffer holding the captured response.</param>
        /// <param name="originalBody">Stream that must be reinstated.</param>
        /// <returns>A task representing the flush operation.</returns>
        private static async Task FlushAndRestoreAsync(HttpContext context, MemoryStream buffer, Stream originalBody)
        {
            try
            {
                if (!ReferenceEquals(context.Response.Body, originalBody) && buffer.CanRead)
                {
                    buffer.Position = 0;
                    await buffer.CopyToAsync(originalBody);
                }
            }
            catch
            {
                // The client may already be gone; the response cannot be repaired either way.
            }
            finally
            {
                context.Response.Body = originalBody;

                try
                {
                    await buffer.DisposeAsync();
                }
                catch
                {
                    // Disposing an in-memory stream never changes the outcome.
                }
            }
        }

        /// <summary>
        /// Reads and masks the request body. Only JSON bodies are logged: form encoded payloads
        /// (for example login credentials) cannot be reliably masked and are therefore summarized.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        /// <returns>The masked body, a summary marker, or null when there is no body.</returns>
        private async Task<string?> ReadRequestBodyAsync(HttpContext context)
        {
            try
            {
                var contentLength = context.Request.ContentLength;

                if (contentLength is null or 0)
                    return null;

                if (!IsJsonContent(context.Request.ContentType))
                    return SummarizeOmitted($"content type '{context.Request.ContentType}'");

                if (contentLength > MaxBodyBytes)
                    return SummarizeOmitted($"{contentLength} bytes");

                // Buffering keeps the body readable for model binding, büt is capped so a
                // caller cannot force the server to buffer an unbounded request body.
                context.Request.EnableBuffering(MaxBodyBytes);
                context.Request.Body.Position = 0;

                using var reader = new StreamReader(
                    context.Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

                var raw = await reader.ReadToEndAsync(context.RequestAborted);

                context.Request.Body.Position = 0;

                return SensitiveDataMasker.MaskJson(raw, _sensitiveFields, MaxBodyBytes);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Reads and masks the buffered response body.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        /// <param name="buffer">Buffer holding the captured response.</param>
        /// <returns>The masked body, a summary marker, or null when there is no body.</returns>
        private string? ReadResponseBody(HttpContext context, MemoryStream buffer)
        {
            try
            {
                var contentType = context.Response.ContentType;

                if (!IsJsonContent(contentType))
                {
                    return contentType is null
                        ? null
                        : SummarizeOmitted($"content type '{contentType}'");
                }

                if (buffer.Length == 0)
                    return null;

                if (buffer.Length > MaxBodyBytes)
                    return SummarizeOmitted($"{buffer.Length} bytes");

                buffer.Position = 0;

                var bytes = new byte[buffer.Length];
                var read = buffer.Read(bytes, 0, bytes.Length);

                buffer.Position = 0;

                var raw = Encoding.UTF8.GetString(bytes, 0, read);

                return SensitiveDataMasker.MaskJson(raw, _sensitiveFields, MaxBodyBytes);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Emits the request/response entry.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        /// <param name="startedAt">Timestamp captured when the request entered the middleware.</param>
        /// <param name="requestBody">Masked request body.</param>
        /// <param name="responseBody">Masked response body.</param>
        /// <param name="exception">Exception escaping the pipeline, when any.</param>
        private void Log(
            HttpContext context,
            long startedAt,
            string? requestBody,
            string? responseBody,
            Exception? exception)
        {
            try
            {
                var statusCode = context.Response.StatusCode;
                var durationMs = Math.Round(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds, 1);

                _logger.Log(
                    ResolveLevel(statusCode),
                    exception,
                    LogTemplate,
                    context.Request.Method,
                    context.Request.Path.Value ?? "/",
                    statusCode,
                    durationMs,
                    ResolveCorrelationId(context),
                    ResolveUserId(context),
                    context.Request.QueryString.Value ?? string.Empty,
                    requestBody ?? string.Empty,
                    responseBody ?? string.Empty);
            }
            catch
            {
                // Logging is best effort; the response has already been produced.
            }
        }

        /// <summary>
        /// Maps an HTTP status code onto a log level: server errors are failures, client errors are warnings.
        /// </summary>
        /// <param name="statusCode">Response status code.</param>
        /// <returns>The level used for the entry.</returns>
        private static LogLevel ResolveLevel(int statusCode) => statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        /// <summary>
        /// Reads the correlation id assigned by <see cref="CorrelationIdMiddleware"/>.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        /// <returns>The correlation id in use for the request.</returns>
        private static string ResolveCorrelationId(HttpContext context)
        {
            if (context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var value)
                && value is string correlationId
                && !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }

            return context.TraceIdentifier;
        }

        /// <summary>
        /// Reads the caller identifier from the authenticated principal.
        /// </summary>
        /// <param name="context">Current HTTP context.</param>
        /// <returns>The user id, or an empty string for anonymous requests.</returns>
        private static string ResolveUserId(HttpContext context)
        {
            var principal = context.User;

            if (principal?.Identity?.IsAuthenticated != true)
                return string.Empty;

            return principal.FindFirst(UserIdClaimType)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? string.Empty;
        }

        /// <summary>
        /// Determines whether a content type carries JSON, which is the only body format this
        /// middleware can mask structurally.
        /// </summary>
        /// <param name="contentType">Content type header value.</param>
        /// <returns><c>true</c> when the body can be parsed and masked.</returns>
        private static bool IsJsonContent(string? contentType)
            => !string.IsNullOrWhiteSpace(contentType)
               && contentType.Contains("json", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Builds a marker describing why a body was not logged.
        /// </summary>
        /// <param name="reason">Reason shown to the log reader.</param>
        /// <returns>A short summary marker.</returns>
        private static string SummarizeOmitted(string reason)
            => $"<body_not_logged: {reason}>";

        /// <summary>
        /// Resolves the effective masking field list, falling back to the built-in defaults when
        /// configuration cannot be read.
        /// </summary>
        /// <param name="configuration">Application configuration root.</param>
        /// <returns>The field names that must be masked.</returns>
        private static IReadOnlyCollection<string> ResolveSensitiveFields(IConfiguration configuration)
        {
            try
            {
                return GelfOptions.FromConfiguration(configuration).EffectiveMaskedFields;
            }
            catch
            {
                // A configuration problem must not disable masking.
                return GelfOptions.DefaultSensitiveFields;
            }
        }
    }
}
