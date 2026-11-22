namespace Rezervise.Api.Logging
{
    /// <summary>
    /// Non-blocking hand-off point between the logger and the UDP transport.
    /// Implementations must never throw and must never block the calling thread.
    /// </summary>
    internal interface IGelfSink
    {
        /// <summary>
        /// Attempts to enqueue a serialized GELF payload for background delivery.
        /// </summary>
        /// <param name="payload">UTF-8 encoded GELF document.</param>
        /// <returns><c>true</c> when the payload was accepted; <c>false</c> when it was dropped.</returns>
        bool TryEnqueue(byte[] payload);
    }
}
