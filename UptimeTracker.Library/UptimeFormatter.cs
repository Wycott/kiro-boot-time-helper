namespace UptimeTracker;

internal static class UptimeFormatter
{
    /// <summary>Formats uptime as "Xd HHh MMm SSs", e.g. "3d 04h 22m 11s".</summary>
    public static string Format(TimeSpan uptime)
    {
        int days = (int)uptime.TotalDays;
        int hours = uptime.Hours;
        int minutes = uptime.Minutes;
        int seconds = uptime.Seconds;

        return $"{days}d {hours:D2}h {minutes:D2}m {seconds:D2}s";
    }
}
