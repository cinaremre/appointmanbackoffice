namespace AppointMan.Application.Common.Helpers
{
    /// <summary>
    /// Translates <see cref="DayOfWeek"/> values to Turkish display names.
    /// .NET renders the enum in English, which does not match the language of the API.
    /// </summary>
    public static class DayNameHelper
    {
        /// <summary>
        /// Returns the Turkish name for the given day. Undefined values yield an
        /// empty string so callers never receive an English fallback.
        /// </summary>
        /// <param name="dayOfWeek">Day value, Sunday = 0 through Saturday = 6.</param>
        /// <returns>Turkish day name, or an empty string when the value is undefined.</returns>
        public static string ToTurkishName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Pazartesi",
                DayOfWeek.Tuesday => "Salı",
                DayOfWeek.Wednesday => "Çarşamba",
                DayOfWeek.Thursday => "Perşembe",
                DayOfWeek.Friday => "Cuma",
                DayOfWeek.Saturday => "Cumartesi",
                DayOfWeek.Sunday => "Pazar",
                _ => string.Empty
            };
        }
    }
}
