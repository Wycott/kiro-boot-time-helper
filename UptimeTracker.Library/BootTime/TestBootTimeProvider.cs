namespace UptimeTracker.BootTime;

internal sealed class TestBootTimeProvider : IBootTimeProvider
{
    private readonly DateTime startTime;

    public TestBootTimeProvider(DateTime startTime)
    {
        this.startTime = startTime;
    }

    public DateTime GetBootTime() => startTime;
}
