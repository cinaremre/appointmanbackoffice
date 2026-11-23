namespace Rezervise.Api.Metrics
{
    /// <summary>
    /// Emits process-level warnings when the metrics pipeline cannot be configured.
    /// Warnings are written to standard error at most önce so a broken exporter cannot flood the output.
    /// </summary>
    internal static class MetricsDiagnostics
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
                Console.Error.WriteLine($"[Metrics] {message}");
            }
            catch
            {
                // Diagnostics must never surface as an application failure.
            }
        }
    }
}
