using UptimeTracker.Models;

namespace UptimeTracker.Tests;

public class ThresholdResolverTests
{
    // Fixed configuration: warn=2h, reboot=6h, overdue=12h with default colors
    private static ThresholdConfiguration DefaultConfig => new ThresholdConfiguration(
        Warn: new WarnThreshold(
            After: TimeSpan.FromHours(2),
            Foreground: ConsoleColor.Yellow,
            Background: null),
        Reboot: new RebootThreshold(
            After: TimeSpan.FromHours(6),
            Foreground: ConsoleColor.Red,
            Background: null),
        Overdue: new OverdueThreshold(
            After: TimeSpan.FromHours(12),
            PairA: new FlashPair(ConsoleColor.Red, ConsoleColor.White),
            PairB: new FlashPair(ConsoleColor.White, ConsoleColor.Red),
            FlashIntervalMs: 1000));

    // ── Boundary tests ──────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_ZeroUptime_ReturnsDefault()
    {
        var result = ThresholdResolver.Resolve(TimeSpan.Zero, DefaultConfig, 0);

        Assert.IsType<ColorState.Default>(result);
    }

    [Fact]
    public void Resolve_WarnMinus1s_ReturnsDefault()
    {
        var uptime = TimeSpan.FromHours(2) - TimeSpan.FromSeconds(1); // 1h 59m 59s
        var result = ThresholdResolver.Resolve(uptime, DefaultConfig, 0);

        Assert.IsType<ColorState.Default>(result);
    }

    [Fact]
    public void Resolve_ExactlyWarn_ReturnsWarn()
    {
        var uptime = TimeSpan.FromHours(2); // 2h 0m 0s
        var result = ThresholdResolver.Resolve(uptime, DefaultConfig, 0);

        Assert.IsType<ColorState.Warn>(result);
    }

    [Fact]
    public void Resolve_WarnPlus1s_ReturnsWarn()
    {
        var uptime = TimeSpan.FromHours(2) + TimeSpan.FromSeconds(1); // 2h 0m 1s
        var result = ThresholdResolver.Resolve(uptime, DefaultConfig, 0);
        
        Assert.IsType<ColorState.Warn>(result);
    }

    [Fact]
    public void Resolve_RebootMinus1s_ReturnsWarn()
    {
        var uptime = TimeSpan.FromHours(6) - TimeSpan.FromSeconds(1); // 5h 59m 59s
        var result = ThresholdResolver.Resolve(uptime, DefaultConfig, 0);

        Assert.IsType<ColorState.Warn>(result);
    }

    [Fact]
    public void Resolve_ExactlyReboot_ReturnsReboot()
    {
        var uptime = TimeSpan.FromHours(6); // 6h 0m 0s
        var result = ThresholdResolver.Resolve(uptime, DefaultConfig, 0);
        
        Assert.IsType<ColorState.Reboot>(result);
    }

    [Fact]
    public void Resolve_RebootPlus1s_ReturnsReboot()
    {
        var uptime = TimeSpan.FromHours(6) + TimeSpan.FromSeconds(1); // 6h 0m 1s
        var result = ThresholdResolver.Resolve(uptime, DefaultConfig, 0);
       
        Assert.IsType<ColorState.Reboot>(result);
    }

    [Fact]
    public void Resolve_OverdueMinus1s_ReturnsReboot()
    {
        var uptime = TimeSpan.FromHours(12) - TimeSpan.FromSeconds(1); // 11h 59m 59s
        var result = ThresholdResolver.Resolve(uptime, DefaultConfig, 0);
        
        Assert.IsType<ColorState.Reboot>(result);
    }

    [Fact]
    public void Resolve_ExactlyOverdue_ReturnsOverdue()
    {
        var uptime = TimeSpan.FromHours(12); // 12h 0m 0s
        var result = ThresholdResolver.Resolve(uptime, DefaultConfig, 0);
        
        Assert.IsType<ColorState.Overdue>(result);
    }

    [Fact]
    public void Resolve_OverduePlus1s_ReturnsOverdue()
    {
        var uptime = TimeSpan.FromHours(12) + TimeSpan.FromSeconds(1); // 12h 0m 1s
        var result = ThresholdResolver.Resolve(uptime, DefaultConfig, 0);
        
        Assert.IsType<ColorState.Overdue>(result);
    }

    // ── Flash alternation tests ──────────────────────────────────────────────────

    private static readonly TimeSpan OverdueUptime = TimeSpan.FromHours(13);

    [Theory]
    [InlineData(0)]   // even → PairA
    [InlineData(2)]   // even → PairA
    [InlineData(100)] // even → PairA
    public void Resolve_EvenFlashTick_ReturnsPairA(int flashTick)
    {
        var result = (ColorState.Overdue)ThresholdResolver.Resolve(OverdueUptime, DefaultConfig, flashTick);

        Assert.Equal(DefaultConfig.Overdue.PairA, result.ActivePair);
    }

    [Theory]
    [InlineData(1)]   // odd → PairB
    [InlineData(3)]   // odd → PairB
    [InlineData(101)] // odd → PairB
    public void Resolve_OddFlashTick_ReturnsPairB(int flashTick)
    {
        var result = (ColorState.Overdue)ThresholdResolver.Resolve(OverdueUptime, DefaultConfig, flashTick);

        Assert.Equal(DefaultConfig.Overdue.PairB, result.ActivePair);
    }

    // ── Color configuration tests ────────────────────────────────────────────────

    [Fact]
    public void Resolve_DefaultColors_WarnUsesYellowForeground()
    {
        var uptime = TimeSpan.FromHours(3);
        var result = (ColorState.Warn)ThresholdResolver.Resolve(uptime, DefaultConfig, 0);

        Assert.Equal(ConsoleColor.Yellow, result.Foreground);
        Assert.Null(result.Background);
    }

    [Fact]
    public void Resolve_DefaultColors_RebootUsesRedForeground()
    {
        var uptime = TimeSpan.FromHours(8);
        var result = (ColorState.Reboot)ThresholdResolver.Resolve(uptime, DefaultConfig, 0);

        Assert.Equal(ConsoleColor.Red, result.Foreground);
        Assert.Null(result.Background);
    }

    [Fact]
    public void Resolve_CustomWarnForeground_AppliesCustomColor()
    {
        var config = DefaultConfig with
        {
            Warn = DefaultConfig.Warn with { Foreground = ConsoleColor.Cyan }
        };
        var uptime = TimeSpan.FromHours(3);
        var result = (ColorState.Warn)ThresholdResolver.Resolve(uptime, config, 0);

        Assert.Equal(ConsoleColor.Cyan, result.Foreground);
    }

    [Fact]
    public void Resolve_CustomRebootBackground_AppliesCustomBackground()
    {
        var config = DefaultConfig with
        {
            Reboot = DefaultConfig.Reboot with { Background = ConsoleColor.DarkBlue }
        };
        var uptime = TimeSpan.FromHours(8);
        var result = (ColorState.Reboot)ThresholdResolver.Resolve(uptime, config, 0);

        Assert.Equal(ConsoleColor.DarkBlue, result.Background);
    }

    [Fact]
    public void Resolve_DefaultFlashPairs_PairAIsRedOnWhite()
    {
        var result = (ColorState.Overdue)ThresholdResolver.Resolve(OverdueUptime, DefaultConfig, 0);

        Assert.Equal(ConsoleColor.Red, result.ActivePair.Foreground);
        Assert.Equal(ConsoleColor.White, result.ActivePair.Background);
    }

    [Fact]
    public void Resolve_DefaultFlashPairs_PairBIsWhiteOnRed()
    {
        var result = (ColorState.Overdue)ThresholdResolver.Resolve(OverdueUptime, DefaultConfig, 1);

        Assert.Equal(ConsoleColor.White, result.ActivePair.Foreground);
        Assert.Equal(ConsoleColor.Red, result.ActivePair.Background);
    }

    [Fact]
    public void Resolve_CustomFlashPairs_AppliesCustomPairs()
    {
        var customPairA = new FlashPair(ConsoleColor.Magenta, ConsoleColor.DarkGreen);
        var customPairB = new FlashPair(ConsoleColor.DarkGreen, ConsoleColor.Magenta);
        var config = DefaultConfig with
        {
            Overdue = DefaultConfig.Overdue with { PairA = customPairA, PairB = customPairB }
        };

        var resultA = (ColorState.Overdue)ThresholdResolver.Resolve(OverdueUptime, config, 0);
        var resultB = (ColorState.Overdue)ThresholdResolver.Resolve(OverdueUptime, config, 1);

        Assert.Equal(customPairA, resultA.ActivePair);
        Assert.Equal(customPairB, resultB.ActivePair);
    }
}
