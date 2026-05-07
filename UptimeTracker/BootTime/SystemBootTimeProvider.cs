namespace UptimeTracker.BootTime;

internal sealed class SystemBootTimeProvider : IBootTimeProvider
{
    public DateTime GetBootTime()
    {
        // Cross-platform: subtract milliseconds-since-boot from current local time.
        return DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount64);
    }
}
