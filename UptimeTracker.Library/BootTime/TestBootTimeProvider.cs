namespace UptimeTracker.BootTime;

internal sealed class TestBootTimeProvider : IBootTimeProvider
{
    private readonly DateTime _startTime;

    public TestBootTimeProvider(DateTime startTime)
    {
        _startTime = startTime;
    }

    public DateTime GetBootTime() => _startTime;
}
