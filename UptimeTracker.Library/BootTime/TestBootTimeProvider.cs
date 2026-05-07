namespace UptimeTracker.BootTime;

internal sealed class TestBootTimeProvider(DateTime startTime) : IBootTimeProvider
{
    public DateTime GetBootTime() => startTime;
}
