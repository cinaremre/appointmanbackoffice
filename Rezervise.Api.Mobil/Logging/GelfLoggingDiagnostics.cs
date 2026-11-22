namespace Rezervise.Api.Logging
{
    /// <summary>
    /// Emits process-level warnings when the logging infrastructure itself fails.
    /// Warnings are written to standard error at most once so a broken sink cannot flood the output.
    /// </summary>
    internal static class GelfLoggingDiagnostics
    {
        private static int _warned;

        /// <summary>
        /// Writes a warning to standard error the first time it is called.
        /// </summary>
        /// <param name="message">Warning text.</param>
        public static void WarnOnce(string message)
        {
            if (Interlocked.Exchange(ref _warned, 1) != 0)
                return;

            try
            {
                Console.Error.WriteLine($"[Graylog] {message}");
            }
            catch
            {
                // Diagnostics must never surface as an application failure.
            }
        }
    }
}
