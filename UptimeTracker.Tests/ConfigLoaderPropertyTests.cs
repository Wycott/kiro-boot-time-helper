// Feature: dotnet-uptime-tracker, Property 4: Config validation rejects invalid 'after' values
// Feature: dotnet-uptime-tracker, Property 5: Config validation rejects invalid ConsoleColor names
// Feature: dotnet-uptime-tracker, Property 6: Config validation enforces ascending threshold order

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using UptimeTracker.Exceptions;
using UptimeTracker.Models;

namespace UptimeTracker.Tests;

public class ConfigLoaderPropertyTests : IDisposable
{
    private readonly string tempDir;

    public ConfigLoaderPropertyTests()
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

    /// <summary>
    /// Returns true if the string is a valid HH:MM:SS duration
    /// (HH non-negative integer, MM 0-59, SS 0-59).
    /// </summary>
    private static bool IsValidAfter(string s)
    {
        var parts = s.Split(':');

        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var h) || h < 0)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var m) || m < 0 || m > 59)
        {
            return false;
        }

        if (!int.TryParse(parts[2], out var sec) || sec < 0 || sec > 59)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns true if the string is a valid ConsoleColor name (case-insensitive).
    /// </summary>
    private static bool IsValidConsoleColor(string s) =>
        Enum.TryParse<ConsoleColor>(s, ignoreCase: true, out _);

    /// <summary>
    /// Formats a TimeSpan as HH:MM:SS where HH can exceed 23.
    /// </summary>
    private static string FormatAfter(TimeSpan ts) =>
        $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";

    // ── Property 4: Invalid 'after' strings always rejected ──────────────────────

    /// <summary>
    /// Validates: Requirements 3.6
    ///
    /// For any string that is not a valid HH:MM:SS duration, ConfigLoader.Load
    /// SHALL throw ConfigurationException when used as the 'after' value for warn.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property InvalidAfterStringsAlwaysRejected()
    {
        // Generator: arbitrary non-null strings filtered to exclude valid HH:MM:SS patterns
        var arb = ArbMap.Default.ArbFor<string>()
            .Generator
            .Where(s => s != null && !IsValidAfter(s))
            .ToArbitrary();

        return Prop.ForAll(arb, invalidAfter =>
        {
            WriteConfig($$"""
                {
                  "warn":   { "after": {{System.Text.Json.JsonSerializer.Serialize(invalidAfter)}} },
                  "reboot": { "after": "06:00:00" },
                  "overdue":{ "after": "12:00:00" }
                }
                """);

            try
            {
                Load();

                return false; // Should have thrown
            }
            catch (ConfigurationException)
            {
                return true; // Expected
            }
        });
    }

    // ── Property 5: Invalid ConsoleColor names always rejected ───────────────────

    /// <summary>
    /// Validates: Requirements 3.7
    ///
    /// For any string that is not a valid ConsoleColor member name (case-insensitive),
    /// ConfigLoader.Load SHALL throw ConfigurationException when used as warn.foreground.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property InvalidConsoleColorNamesAlwaysRejected()
    {
        // Generator: arbitrary non-null strings filtered to exclude valid ConsoleColor names
        var arb = ArbMap.Default.ArbFor<string>()
            .Generator
            .Where(s => s != null && !IsValidConsoleColor(s))
            .ToArbitrary();

        return Prop.ForAll(arb, invalidColor =>
        {
            WriteConfig($$"""
                {
                  "warn":   { "after": "01:00:00", "foreground": {{System.Text.Json.JsonSerializer.Serialize(invalidColor)}} },
                  "reboot": { "after": "06:00:00" },
                  "overdue":{ "after": "12:00:00" }
                }
                """);

            try
            {
                Load();

                return false; // Should have thrown
            }
            catch (ConfigurationException)
            {
                return true; // Expected
            }
        });
    }

    // ── Property 6: Non-ascending threshold order always rejected ────────────────

    /// <summary>
    /// Validates: Requirements 3.3
    ///
    /// For any TimeSpan triple (a, b, c) where a >= b or b >= c,
    /// ConfigLoader.Load SHALL throw ConfigurationException.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property NonAscendingThresholdOrderAlwaysRejected()
    {
        // Generator: three TimeSpan values (in seconds, 0–86400) where NOT strictly ascending
        // We generate (a, b, c) where a >= b OR b >= c
        var arb =
            (from a in Gen.Choose(0, 86400)
             from b in Gen.Choose(0, 86400)
             from c in Gen.Choose(0, 86400)
             where a >= b || b >= c
             select (a, b, c))
            .ToArbitrary();

        return Prop.ForAll(arb, triple =>
        {
            var (a, b, c) = triple;
            var warnAfter = FormatAfter(TimeSpan.FromSeconds(a));
            var rebootAfter = FormatAfter(TimeSpan.FromSeconds(b));
            var overdueAfter = FormatAfter(TimeSpan.FromSeconds(c));

            WriteConfig($$"""
                {
                  "warn":   { "after": "{{warnAfter}}" },
                  "reboot": { "after": "{{rebootAfter}}" },
                  "overdue":{ "after": "{{overdueAfter}}" }
                }
                """);

            try
            {
                Load();

                return false; // Should have thrown
            }
            catch (ConfigurationException)
            {
                return true; // Expected
            }
        });
    }
}
