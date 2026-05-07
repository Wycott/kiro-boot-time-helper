using UptimeTracker.Exceptions;
using UptimeTracker.Models;

namespace UptimeTracker.Tests;

public class ConfigLoaderTests : IDisposable
{
    private readonly string tempDir;

    public ConfigLoaderTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private void WriteConfig(string json)
    {
        File.WriteAllText(Path.Combine(tempDir, "uptime-tracker.json"), json);
    }

    private AppConfiguration Load() => ConfigLoader.Load(tempDir);

    // ── Valid configuration tests ─────────────────────────────────────────────────

    [Fact]
    public void MinimalConfig_LoadsWithAllDefaults()
    {
        WriteConfig("""
            {
              "warn":   { "after": "01:00:00" },
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        var config = Load();

        // Thresholds
        Assert.Equal(TimeSpan.FromHours(1), config.Thresholds.Warn.After);
        Assert.Equal(TimeSpan.FromHours(6), config.Thresholds.Reboot.After);
        Assert.Equal(TimeSpan.FromHours(12), config.Thresholds.Overdue.After);

        // Defaults
        Assert.Equal(ConsoleColor.Yellow, config.Thresholds.Warn.Foreground);
        Assert.Null(config.Thresholds.Warn.Background);
        Assert.Equal(ConsoleColor.Red, config.Thresholds.Reboot.Foreground);
        Assert.Null(config.Thresholds.Reboot.Background);

        // Default flash pairs
        Assert.Equal(new FlashPair(ConsoleColor.Red, ConsoleColor.White), config.Thresholds.Overdue.PairA);
        Assert.Equal(new FlashPair(ConsoleColor.White, ConsoleColor.Red), config.Thresholds.Overdue.PairB);
        Assert.Equal(1000, config.Thresholds.Overdue.FlashIntervalMs);

        // TestMode default
        Assert.False(config.TestMode);
    }

    [Fact]
    public void FullConfig_LoadsWithSpecifiedValues()
    {
        WriteConfig("""
            {
              "testMode": true,
              "warn": {
                "after": "02:00:00",
                "foreground": "Cyan",
                "background": "DarkBlue"
              },
              "reboot": {
                "after": "08:00:00",
                "foreground": "Magenta",
                "background": "DarkRed"
              },
              "overdue": {
                "after": "16:00:00",
                "flash": [
                  { "foreground": "Green", "background": "DarkGreen" },
                  { "foreground": "DarkGreen", "background": "Green" }
                ],
                "flashIntervalMs": 500
              }
            }
            """);

        var config = Load();

        Assert.True(config.TestMode);
        Assert.Equal(TimeSpan.FromHours(2), config.Thresholds.Warn.After);
        Assert.Equal(ConsoleColor.Cyan, config.Thresholds.Warn.Foreground);
        Assert.Equal(ConsoleColor.DarkBlue, config.Thresholds.Warn.Background);

        Assert.Equal(TimeSpan.FromHours(8), config.Thresholds.Reboot.After);
        Assert.Equal(ConsoleColor.Magenta, config.Thresholds.Reboot.Foreground);
        Assert.Equal(ConsoleColor.DarkRed, config.Thresholds.Reboot.Background);

        Assert.Equal(TimeSpan.FromHours(16), config.Thresholds.Overdue.After);
        Assert.Equal(new FlashPair(ConsoleColor.Green, ConsoleColor.DarkGreen), config.Thresholds.Overdue.PairA);
        Assert.Equal(new FlashPair(ConsoleColor.DarkGreen, ConsoleColor.Green), config.Thresholds.Overdue.PairB);
        Assert.Equal(500, config.Thresholds.Overdue.FlashIntervalMs);
    }

    [Fact]
    public void TestModeAbsent_ReturnsFalse()
    {
        WriteConfig("""
            {
              "warn":   { "after": "01:00:00" },
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        var config = Load();
        Assert.False(config.TestMode);
    }

    [Fact]
    public void TestModeFalse_ReturnsFalse()
    {
        WriteConfig("""
            {
              "testMode": false,
              "warn":   { "after": "01:00:00" },
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        var config = Load();
        Assert.False(config.TestMode);
    }

    [Fact]
    public void TestModeTrue_ReturnsTrue()
    {
        WriteConfig("""
            {
              "testMode": true,
              "warn":   { "after": "01:00:00" },
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        var config = Load();
        Assert.True(config.TestMode);
    }

    [Fact]
    public void FlashIntervalMs_One_IsValid()
    {
        WriteConfig("""
            {
              "warn":   { "after": "01:00:00" },
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00", "flashIntervalMs": 1 }
            }
            """);

        var config = Load();
        Assert.Equal(1, config.Thresholds.Overdue.FlashIntervalMs);
    }

    // ── Error case tests ──────────────────────────────────────────────────────────

    [Fact]
    public void ConfigFileNotFound_ThrowsWithFilePath()
    {
        // Don't write any file — directory is empty
        var ex = Assert.Throws<ConfigurationException>(() => Load());
        Assert.Contains(tempDir, ex.Message);
    }

    [Fact]
    public void WarnKeyMissing_ThrowsWithWarnInMessage()
    {
        WriteConfig("""
            {
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        var ex = Assert.Throws<ConfigurationException>(() => Load());
        Assert.Contains("warn", ex.Message);
    }

    [Fact]
    public void RebootKeyMissing_ThrowsWithRebootInMessage()
    {
        WriteConfig("""
            {
              "warn":   { "after": "01:00:00" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        var ex = Assert.Throws<ConfigurationException>(() => Load());
        Assert.Contains("reboot", ex.Message);
    }

    [Fact]
    public void OverdueKeyMissing_ThrowsWithOverdueInMessage()
    {
        WriteConfig("""
            {
              "warn":   { "after": "01:00:00" },
              "reboot": { "after": "06:00:00" }
            }
            """);

        var ex = Assert.Throws<ConfigurationException>(() => Load());
        Assert.Contains("overdue", ex.Message);
    }

    [Fact]
    public void AfterMissingFromWarn_ThrowsConfigurationException()
    {
        WriteConfig("""
            {
              "warn":   { },
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        Assert.Throws<ConfigurationException>(() => Load());
    }

    [Fact]
    public void AfterValueAbc_ThrowsConfigurationException()
    {
        WriteConfig("""
            {
              "warn":   { "after": "abc" },
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        Assert.Throws<ConfigurationException>(() => Load());
    }

    [Fact]
    public void AfterValueNegativeHours_ThrowsConfigurationException()
    {
        WriteConfig("""
            {
              "warn":   { "after": "-01:00:00" },
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        Assert.Throws<ConfigurationException>(() => Load());
    }

    [Fact]
    public void AfterValueMinutesOutOfRange_ThrowsConfigurationException()
    {
        WriteConfig("""
            {
              "warn":   { "after": "00:60:00" },
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        Assert.Throws<ConfigurationException>(() => Load());
    }

    [Fact]
    public void WarnAfterGreaterThanOrEqualRebootAfter_ThrowsConfigurationException()
    {
        WriteConfig("""
            {
              "warn":   { "after": "06:00:00" },
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        Assert.Throws<ConfigurationException>(() => Load());
    }

    [Fact]
    public void RebootAfterGreaterThanOrEqualOverdueAfter_ThrowsConfigurationException()
    {
        WriteConfig("""
            {
              "warn":   { "after": "01:00:00" },
              "reboot": { "after": "12:00:00" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        Assert.Throws<ConfigurationException>(() => Load());
    }

    [Fact]
    public void WarnForegroundInvalidColor_ThrowsWithColorNameInMessage()
    {
        WriteConfig("""
            {
              "warn":   { "after": "01:00:00", "foreground": "NotAColor" },
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        var ex = Assert.Throws<ConfigurationException>(() => Load());
        Assert.Contains("NotAColor", ex.Message);
    }

    [Fact]
    public void RebootBackgroundInvalidColor_ThrowsConfigurationException()
    {
        WriteConfig("""
            {
              "warn":   { "after": "01:00:00" },
              "reboot": { "after": "06:00:00", "background": "NotAColor" },
              "overdue":{ "after": "12:00:00" }
            }
            """);

        Assert.Throws<ConfigurationException>(() => Load());
    }

    [Fact]
    public void FlashPairForegroundInvalidColor_ThrowsConfigurationException()
    {
        WriteConfig("""
            {
              "warn":   { "after": "01:00:00" },
              "reboot": { "after": "06:00:00" },
              "overdue":{
                "after": "12:00:00",
                "flash": [
                  { "foreground": "NotAColor", "background": "White" },
                  { "foreground": "White", "background": "Red" }
                ]
              }
            }
            """);

        Assert.Throws<ConfigurationException>(() => Load());
    }

    [Fact]
    public void FlashIntervalMsZero_ThrowsConfigurationException()
    {
        WriteConfig("""
            {
              "warn":   { "after": "01:00:00" },
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00", "flashIntervalMs": 0 }
            }
            """);

        Assert.Throws<ConfigurationException>(() => Load());
    }

    [Fact]
    public void FlashIntervalMsNegative_ThrowsConfigurationException()
    {
        WriteConfig("""
            {
              "warn":   { "after": "01:00:00" },
              "reboot": { "after": "06:00:00" },
              "overdue":{ "after": "12:00:00", "flashIntervalMs": -1 }
            }
            """);

        Assert.Throws<ConfigurationException>(() => Load());
    }
}
