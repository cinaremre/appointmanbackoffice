namespace Rezervise.Application.Common.Exceptions
{
    /// <summary>
    /// Thrown when an authenticated caller tries to access a resource they do not own.
    /// Mapped to HTTP 403 by the API exception middleware.
    /// </summary>
    public sealed class ForbiddenAccessException : Exception
    {
        /// <summary>
        /// İnitialıses the exception with a safe message that may be surfaced to the caller.
        /// </summary>
        /// <param name="message">Safe message; a generic default is used when omitted.</param>
        public ForbiddenAccessException(string? message = null)
            : base(message ?? "Access to the requested resource is not allowed.")
        {
        }
    }
}
