namespace UptimeTracker;

internal static class UptimeFormatter
{
    /// <summary>Formats uptime as "Xd HHh MMm SSs", e.g. "3d 04h 22m 11s".</summary>
    public static string Format(TimeSpan uptime)
    {
        var days = (int)uptime.TotalDays;
        var hours = uptime.Hours;
        var minutes = uptime.Minutes;
        var seconds = uptime.Seconds;

        return $"{days}d {hours:D2}h {minutes:D2}m {seconds:D2}s";
    }
}
