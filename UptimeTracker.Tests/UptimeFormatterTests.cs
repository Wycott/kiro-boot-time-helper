using UptimeTracker;

namespace UptimeTracker.Tests;

public class UptimeFormatterTests
{
    [Fact]
    public void Format_Zero_ReturnsZeroString()
    {
        Assert.Equal("0d 00h 00m 00s", UptimeFormatter.Format(TimeSpan.Zero));
    }

    [Fact]
    public void Format_OneSecond_ReturnsCorrectString()
    {
        Assert.Equal("0d 00h 00m 01s", UptimeFormatter.Format(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Format_OneMinute_ReturnsCorrectString()
    {
        Assert.Equal("0d 00h 01m 00s", UptimeFormatter.Format(TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Format_OneHour_ReturnsCorrectString()
    {
        Assert.Equal("0d 01h 00m 00s", UptimeFormatter.Format(TimeSpan.FromHours(1)));
    }

    [Fact]
    public void Format_OneDay_ReturnsCorrectString()
    {
        Assert.Equal("1d 00h 00m 00s", UptimeFormatter.Format(TimeSpan.FromDays(1)));
    }

    [Fact]
    public void Format_3Days4Hours22Minutes11Seconds_ReturnsCorrectString()
    {
        var uptime = new TimeSpan(days: 3, hours: 4, minutes: 22, seconds: 11);
        Assert.Equal("3d 04h 22m 11s", UptimeFormatter.Format(uptime));
    }

    [Fact]
    public void Format_999Days23Hours59Minutes59Seconds_ReturnsCorrectString()
    {
        var uptime = new TimeSpan(days: 999, hours: 23, minutes: 59, seconds: 59);
        Assert.Equal("999d 23h 59m 59s", UptimeFormatter.Format(uptime));
    }
}
